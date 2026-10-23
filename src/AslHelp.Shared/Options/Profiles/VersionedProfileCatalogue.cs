using AslHelp.Logging;
using AslHelp.Options.Binding;
using System;
using System.Linq;
using System.Reflection;

namespace AslHelp.Options.Profiles;

/// <summary>
///     Provides a store of profiles keyed by the version each profile applies from and the pointer size it is laid out
///     for. A lookup returns the newest entry at or below a target's version.
/// </summary>
/// <remarks>
///     Some catalogues split their entries by a partition in front of the version. The shipped entries are loaded from
///     files called <c>[&lt;partition&gt;-]&lt;version&gt;-&lt;x86|x64&gt;</c>. Every error and log line describes a
///     key the same way. A deriving catalogue adds what its keys mean and the methods a caller uses.
/// </remarks>
/// <typeparam name="TOptions">The options type the profiles bind to.</typeparam>
public abstract class VersionedProfileCatalogue<TOptions> : ProfileCatalogue<ProfileKey, TOptions>
    where TOptions : class
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="VersionedProfileCatalogue{TOptions}"/> class.
    /// </summary>
    /// <param name="engine">The subject of the entries as an error writes it, such as <c>"Widget"</c>.</param>
    /// <param name="entryLabel">The word a log line uses for an entry, such as <c>"widget"</c>.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="engine"/> or <paramref name="entryLabel"/> is <see langword="null"/>.
    /// </exception>
    protected VersionedProfileCatalogue(string engine, string entryLabel)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(entryLabel);

        Engine = engine;
        EntryLabel = entryLabel;
    }

    /// <summary>
    ///     Gets the subject of the entries as an error writes it.
    /// </summary>
    protected string Engine { get; }

    /// <summary>
    ///     Gets the word a log line uses for an entry.
    /// </summary>
    protected string EntryLabel { get; }

    /// <summary>
    ///     Creates the key of an entry after checking the pointer size.
    /// </summary>
    /// <param name="partition">The partition. Empty for a catalogue without partitions.</param>
    /// <param name="version">The version the entry applies from.</param>
    /// <param name="pointerSize">The pointer size, 4 or 8.</param>
    /// <returns>
    ///     The key.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="partition"/> or <paramref name="version"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="pointerSize"/> is neither 4 nor 8.
    /// </exception>
    protected static ProfileKey Keyed(string partition, string version, int pointerSize)
    {
        ArgumentNullException.ThrowIfNull(partition);
        ArgumentNullException.ThrowIfNull(version);

        if (pointerSize is not (4 or 8))
        {
            throw new ArgumentOutOfRangeException(
                nameof(pointerSize), pointerSize, "An entry is laid out for either 4 or 8 byte pointers.");
        }

        return new ProfileKey(partition, version, pointerSize);
    }

    /// <summary>
    ///     Registers every entry an assembly ships under a folder. Also registers the fallbacks of the document
    ///     called <paramref name="fallbacks"/> under <c>Profiles/Fallbacks</c>.
    /// </summary>
    /// <param name="assembly">The assembly that contains the entries as embedded resources.</param>
    /// <param name="folder">The folder of the entries, such as <c>"Profiles/Widget"</c>.</param>
    /// <param name="partitioned">Whether an entry's file name starts with a partition.</param>
    /// <param name="fallbacks">The file name of the fallback document under <c>Profiles/Fallbacks</c>.</param>
    /// <exception cref="InvalidOperationException">
    ///     A shipped entry is not called <c>[&lt;partition&gt;-]&lt;version&gt;-&lt;x86|x64&gt;</c>.
    /// </exception>
    protected void LoadShipped(Assembly assembly, string folder, bool partitioned, string fallbacks)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(fallbacks);

        foreach (var (name, jsonc) in OptionsBinder.EnumerateShipped(assembly, folder))
        {
            if (!ProfileKey.TryParseShippedName(name, partitioned, out var key))
            {
                throw new InvalidOperationException(
                    $"The profile '{name}' is not called '{(partitioned ? "<partition>-" : "")}<version>-<x86|x64>'.");
            }

            AddShipped(key, jsonc);
        }

        foreach (var (name, jsonc) in OptionsBinder.EnumerateShipped(assembly, "Profiles/Fallbacks"))
        {
            if (name == fallbacks)
            {
                AddFallbacks(jsonc);
            }
        }
    }

    /// <summary>
    ///     Registers a shipped entry from its document. A catalogue that keeps more than the profile from a document
    ///     overrides this method.
    /// </summary>
    /// <param name="key">The entry's key.</param>
    /// <param name="jsonc">The document.</param>
    protected virtual void AddShipped(ProfileKey key, string jsonc)
    {
        Add(key, jsonc);
    }

    /// <summary>
    ///     Chooses the newest entry at or below a target's version among the entries of the target's partition and
    ///     pointer size.
    /// </summary>
    /// <param name="partition">The target's partition. Empty for a catalogue without partitions.</param>
    /// <param name="version">The target's version.</param>
    /// <param name="pointerSize">The target's pointer size.</param>
    /// <param name="chosen">The entry's key when an entry applies.</param>
    /// <param name="applies">A further test an entry has to pass. <see langword="null"/> for none.</param>
    /// <returns>
    ///     <see langword="true"/> if an entry applies, otherwise <see langword="false"/>.
    /// </returns>
    protected bool TryChoose(string partition, string version, int pointerSize, out ProfileKey chosen, Func<ProfileKey, bool>? applies = null)
    {
        var keys = Entries
            .Select(static entry => entry.Key)
            .Where(key => key.PointerSize == pointerSize
                && string.Equals(key.Partition, partition, StringComparison.OrdinalIgnoreCase)
                && (applies is null || applies(key)));

        return ProfileVersions.TryNewestAtOrBelow(keys, static key => key.Version, version, out chosen);
    }

    /// <summary>
    ///     Creates the failed result of a lookup no entry covers.
    /// </summary>
    /// <param name="partition">The target's partition. Empty for a catalogue without partitions.</param>
    /// <param name="version">The target's version.</param>
    /// <param name="pointerSize">The target's pointer size.</param>
    /// <returns>
    ///     The failed result, with the engine, the target and the pointer size in its message.
    /// </returns>
    protected Result<TOptions> NoProfile(string partition, string version, int pointerSize)
    {
        ArgumentNullException.ThrowIfNull(partition);
        ArgumentNullException.ThrowIfNull(version);

        return ProfileCatalogueError.NoProfile(Engine, partition.Length == 0 ? version : $"{partition} {version}", pointerSize);
    }

    /// <summary>
    ///     Resolves a lookup to a chosen entry. Logs the chosen entry, warns when the target falls in a fallback
    ///     interval and copies the overrides over the entry.
    /// </summary>
    /// <param name="chosen">The entry's key.</param>
    /// <param name="version">The target's version, written into the fallback warning.</param>
    /// <param name="lane">The lane the fallbacks are registered under.</param>
    /// <param name="laneLabel">The lane as a person reads it, such as <c>"Widget x64"</c>.</param>
    /// <param name="overrides">
    ///     The values to copy over the entry. <see langword="null"/> to take the entry unchanged.
    /// </param>
    /// <param name="log">The logger the lookup writes to. <see langword="null"/> writes nothing.</param>
    /// <param name="note">The text the log line adds after the entry. <see langword="null"/> for none.</param>
    /// <param name="plant">
    ///     The action that sets the entry's own values over a fresh parse before the overrides.
    ///     <see langword="null"/> for none.
    /// </param>
    /// <returns>
    ///     The profile on success, otherwise a failed result.
    /// </returns>
    protected Result<TOptions> ResolveChosen(
        ProfileKey chosen,
        string version,
        string lane,
        string laneLabel,
        TOptions? overrides,
        Logger? log,
        string? note = null,
        Action<TOptions>? plant = null)
    {
        // Logs the chosen entry because an entry applies from its version onward, so the target's version alone
        // doesn't show which entry matched.
        log?.LogDebug($"{EntryLabel} entry {Describe(chosen)}{note}");
        WarnOnFallback(log, lane, laneLabel, version);

        return Resolve(chosen, overrides, plant);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     A key reads the way an entry's file is called, such as <c>legacy-2021.3-x64</c>.
    /// </remarks>
    protected sealed override string Describe(ProfileKey key)
    {
        string lane = ProfileKey.LaneOf(key.PointerSize);

        return key.Partition.Length == 0 ? $"{key.Version}-{lane}" : $"{key.Partition}-{key.Version}-{lane}";
    }
}
