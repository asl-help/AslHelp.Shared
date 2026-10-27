using AslHelp.Logging;
using AslHelp.Options.Binding;
using AslHelp.Options.Profiles.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AslHelp.Options.Profiles;

/// <summary>
///     Provides a store of profiles keyed by the build each profile was measured against.
/// </summary>
/// <remarks>
///     A profile is an options document measured against 1 build. The catalogue validates every profile as a whole
///     when the profile is registered. Registering a key again replaces the entry. An entry registered from its
///     document keeps the document so a lookup with overrides copies the override values over a fresh parse and the
///     registered entry stays unchanged. The implementing type decides what a key is, how a key reads in an error and
///     what makes a profile valid.
/// </remarks>
/// <typeparam name="TKey">The type that identifies the build an entry was measured against.</typeparam>
/// <typeparam name="TOptions">The options type the profiles bind to.</typeparam>
public abstract class ProfileCatalogue<TKey, TOptions>
    where TKey : notnull
    where TOptions : class
{
    private readonly Dictionary<TKey, Entry> _entries = [];
    private readonly Dictionary<string, List<ProfileFallback>> _fallbacks = [];

    /// <summary>
    ///     Gets each registered key with its profile.
    /// </summary>
    protected IEnumerable<(TKey Key, TOptions Profile)> Entries
    {
        get
        {
            foreach (var pair in _entries)
            {
                yield return (pair.Key, pair.Value.Profile);
            }
        }
    }

    /// <summary>
    ///     Parses a profile document and validates the whole profile.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>
    ///     The profile.
    /// </returns>
    protected abstract TOptions Parse(string document);

    /// <summary>
    ///     Validates the whole profile, so a profile that can't work fails when it enters the catalogue instead of at
    ///     its first use.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <param name="origin">Where the profile came from, written into the error.</param>
    protected abstract void Validate(TOptions profile, string origin);

    /// <summary>
    ///     Formats a key the way an error about its entry writes it.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>
    ///     The description, such as <c>"legacy-2021.3-x64"</c>.
    /// </returns>
    protected abstract string GetDisplayName(TKey key);

    /// <summary>
    ///     Registers a profile from its document, replacing any entry registered for the key.
    /// </summary>
    /// <param name="key">The key of the build.</param>
    /// <param name="document">The profile document, kept on the entry for overrides to parse again.</param>
    protected void Add(TKey key, string document)
    {
        _entries[key] = new Entry(document, Parse(document));
    }

    /// <summary>
    ///     Registers a profile already parsed from its document, replacing any entry registered for the key. The entry
    ///     keeps the document for overrides to parse again.
    /// </summary>
    /// <param name="key">The key of the build.</param>
    /// <param name="document">The profile document.</param>
    /// <param name="profile">The profile parsed from <paramref name="document"/>.</param>
    protected void Add(TKey key, string document, TOptions profile)
    {
        _entries[key] = new Entry(document, profile);
    }

    /// <summary>
    ///     Registers a parsed profile, replacing any entry registered for the key.
    /// </summary>
    /// <param name="key">The key of the build.</param>
    /// <param name="profile">The profile.</param>
    protected void Add(TKey key, TOptions profile)
    {
        Validate(profile, $"The {GetDisplayName(key)} profile");

        _entries[key] = new Entry(null, profile);
    }

    /// <summary>
    ///     Gets the document an entry was registered from, for an implementing type that merges more than
    ///     <see cref="Resolve"/> copies.
    /// </summary>
    /// <param name="key">The key, already looked up or chosen by the caller.</param>
    /// <returns>
    ///     The document. <see langword="null"/> for an entry registered as a parsed profile.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    ///     No entry is registered for <paramref name="key"/>.
    /// </exception>
    protected string? GetDocument(TKey key)
    {
        return _entries[key].Document;
    }

    /// <summary>
    ///     Gets a value indicating whether the catalogue contains an entry for a key.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>
    ///     <see langword="true"/> if the catalogue contains an entry for <paramref name="key"/>, otherwise
    ///     <see langword="false"/>.
    /// </returns>
    protected bool ContainsKey(TKey key)
    {
        return _entries.ContainsKey(key);
    }

    /// <summary>
    ///     Resolves a registered entry. When <paramref name="configure"/> or <paramref name="overrides"/> is given,
    ///     copies the values of each over a fresh parse of the entry's document.
    /// </summary>
    /// <param name="key">The key, already looked up or chosen by the caller.</param>
    /// <param name="overrides">
    ///     The values to copy over the entry. <see langword="null"/> to take the entry unchanged.
    /// </param>
    /// <param name="configure">
    ///     The action that sets the entry's own values before the overrides, such as the values the entry scopes to a
    ///     backend. <see langword="null"/> for none.
    /// </param>
    /// <returns>
    ///     The profile on success, otherwise a failed result.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    ///     No entry is registered for <paramref name="key"/>.
    /// </exception>
    protected Result<TOptions> Resolve(TKey key, TOptions? overrides, Action<TOptions>? configure = null)
    {
        var entry = _entries[key];

        if (overrides is null && configure is null)
        {
            return entry.Profile;
        }

        if (entry.Document is not { } document)
        {
            return ProfileCatalogueError.DocumentNotRegistered(GetDisplayName(key));
        }

        // Parses the document again so the registered entry stays unchanged. Overrides go last so an explicit override
        // wins over the configured values.
        var merged = Parse(document);
        configure?.Invoke(merged);

        if (overrides is null)
        {
            Validate(merged, $"The {GetDisplayName(key)} profile");

            return merged;
        }

        // Returns a failed result for overrides that break the profile because the overrides come from the caller's
        // input. A registered entry that doesn't validate is a bug and still throws.
        try
        {
            OptionsBinder.ApplyOverrides(merged, overrides);
            Validate(merged, $"The {GetDisplayName(key)} profile");
        }
        catch (JsonException e)
        {
            return ProfileCatalogueError.OverridesRejected(e.Message);
        }

        return merged;
    }

    /// <summary>
    ///     Registers an interval of versions that an architecture serves with the data of another version, so a target
    ///     in the interval gets a warning.
    /// </summary>
    /// <param name="architecture">
    ///     The architecture as an entry's file name writes it after the version, such as <c>"x64"</c>.
    /// </param>
    /// <param name="fallback">The interval.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="architecture"/> or <paramref name="fallback"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    ///     The interval ends at or before its start. Also thrown when the interval overlaps another interval of the
    ///     architecture.
    /// </exception>
    protected void AddFallback(string architecture, ProfileFallback fallback)
    {
        ArgumentNullException.ThrowIfNull(architecture);
        ArgumentNullException.ThrowIfNull(fallback);

        if (ProfileVersions.Compare(ProfileVersions.GetComponents(fallback.From), ProfileVersions.GetComponents(fallback.Until)) >= 0)
        {
            throw new JsonException($"The {architecture} fallback from {fallback.From} ends at {fallback.Until}, which is not after it.");
        }

        if (!_fallbacks.TryGetValue(architecture, out var intervals))
        {
            intervals = [];
            _fallbacks[architecture] = intervals;
        }

        if (intervals.FirstOrDefault(other => other.Contains(fallback.From) || fallback.Contains(other.From)) is { } overlapped)
        {
            throw new JsonException(
                $"The {architecture} fallback from {fallback.From} overlaps the one from {overlapped.From} until {overlapped.Until}.");
        }

        intervals.Add(fallback);
    }

    /// <summary>
    ///     Registers every interval a fallback document lists, keyed by architecture.
    /// </summary>
    /// <param name="document">The document, an object of architectures with a list of intervals each.</param>
    /// <exception cref="JsonException">
    ///     The document or an interval in the document is malformed.
    /// </exception>
    protected void AddFallbacks(string document)
    {
        var architectures = OptionsBinder.Deserialize<Dictionary<string, List<ProfileFallback>>>(document, "$schema") ?? [];

        foreach (var architecture in architectures)
        {
            foreach (var fallback in architecture.Value)
            {
                AddFallback(architecture.Key, fallback);
            }
        }
    }

    /// <summary>
    ///     Writes a warning when a target falls in an interval its architecture serves with the data of another version.
    ///     The warning gives the target's version and the version the data came from.
    /// </summary>
    /// <param name="logger">The logger the warning goes to. <see langword="null"/> writes nothing.</param>
    /// <param name="architecture">The architecture as an entry's file name writes it after the version.</param>
    /// <param name="architectureLabel">The architecture as a person reads it, such as <c>"Widget x64"</c>.</param>
    /// <param name="version">The target's complete version.</param>
    protected void LogFallbackWarning(Logger? logger, string architecture, string architectureLabel, string version)
    {
        if (logger is null || !_fallbacks.TryGetValue(architecture, out var intervals))
        {
            return;
        }

        if (intervals.FirstOrDefault(fallback => fallback.Contains(version)) is { } covering)
        {
            logger.LogWarning($"Detected {architectureLabel} {version}. Using data from {covering.SourceVersion}. This may be incorrect.");
        }
    }

    // Stores a registered profile and the document it was parsed from, if any.
    private sealed record Entry(
        string? Document,
        TOptions Profile);
}
