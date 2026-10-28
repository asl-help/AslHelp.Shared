using AslHelp.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace AslHelp.Options.Binding;

/// <summary>
///     Provides the grammar every options document is read with. Binds documents to options types, applies one
///     bound object's overrides onto another and reads the documents an assembly embeds.
/// </summary>
public static class OptionsBinder
{
    /// <summary>
    ///     Gets the serializer options every document binds with: JSONC, keys in the camel case of an options type's own
    ///     member identifiers and whole numbers read as JSON numbers or <c>"0x"</c> hex strings.
    /// </summary>
    /// <remarks>
    ///     A schema is written with the same options, so the schema and the binder spell keys the same way.
    /// </remarks>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    // Reads a document the way the grammar does, so a parse and a bind agree on comments and trailing commas.
    private static readonly JsonDocumentOptions _jsonc = new()
    {
        CommentHandling = Options.ReadCommentHandling,
        AllowTrailingCommas = Options.AllowTrailingCommas,
    };

    /// <summary>
    ///     Binds a document to <typeparamref name="T"/> after checking that every key is one the serializer binds, so
    ///     a typo in a hand edit fails rather than binding to nothing.
    /// </summary>
    /// <remarks>
    ///     The check is <see cref="OptionsValidator.ValidateKeys"/>, the same one every options file goes through.
    /// </remarks>
    /// <typeparam name="T">The options type the document binds to.</typeparam>
    /// <param name="jsonc">The document.</param>
    /// <param name="envelope">The keys the document adds around the type at its root, such as <c>$schema</c>.</param>
    /// <returns>
    ///     The bound options. <see langword="null"/> when the document is <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="jsonc"/> or <paramref name="envelope"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    ///     The document is malformed or has a key no member takes.
    /// </exception>
    public static T? Deserialize<T>(string jsonc, params string[] envelope)
    {
        ArgumentNullException.ThrowIfNull(jsonc);
        ArgumentNullException.ThrowIfNull(envelope);

        using var document = JsonDocument.Parse(jsonc, _jsonc);

        return Deserialize<T>(document.RootElement, envelope);
    }

    /// <summary>
    ///     Binds a parsed document to <typeparamref name="T"/> after checking that every key is one the serializer
    ///     binds.
    /// </summary>
    /// <typeparam name="T">The options type the document binds to.</typeparam>
    /// <param name="root">The document.</param>
    /// <param name="envelope">The keys the document adds around the type at its root, such as <c>$schema</c>.</param>
    /// <returns>
    ///     The bound options. <see langword="null"/> when the document is <c>null</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="envelope"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    ///     The document has a key no member takes.
    /// </exception>
    public static T? Deserialize<T>(JsonElement root, params string[] envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        OptionsValidator.ValidateKeys(root, typeof(T), Options, "document", envelope);

        return root.Deserialize<T>(Options);
    }

    /// <summary>
    ///     Reads a profile from a document. Binds the document, refuses an empty document and validates the whole
    ///     profile.
    /// </summary>
    /// <typeparam name="T">The profile's options type.</typeparam>
    /// <param name="jsonc">The document.</param>
    /// <param name="validate">The check the profile goes through, called with the origin <c>"The profile"</c>.</param>
    /// <param name="envelope">The keys the document adds around the type at its root, such as <c>$schema</c>.</param>
    /// <returns>
    ///     The profile.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="jsonc"/>, <paramref name="validate"/> or <paramref name="envelope"/> is
    ///     <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    ///     The document is malformed, is <c>null</c>, has a key no member takes or fails <paramref name="validate"/>.
    /// </exception>
    public static T ParseProfile<T>(string jsonc, Action<T, string> validate, params string[] envelope)
    {
        ArgumentNullException.ThrowIfNull(jsonc);
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(envelope);

        using var document = JsonDocument.Parse(jsonc, _jsonc);

        return ParseProfile(document.RootElement, validate, envelope);
    }

    /// <summary>
    ///     Reads a profile from a parsed document. Binds the document, refuses an empty document and validates the
    ///     whole profile.
    /// </summary>
    /// <typeparam name="T">The profile's options type.</typeparam>
    /// <param name="root">The document.</param>
    /// <param name="validate">The check the profile goes through, called with the origin <c>"The profile"</c>.</param>
    /// <param name="envelope">The keys the document adds around the type at its root, such as <c>$schema</c>.</param>
    /// <returns>
    ///     The profile.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="validate"/> or <paramref name="envelope"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    ///     The document is <c>null</c>, has a key no member takes or fails <paramref name="validate"/>.
    /// </exception>
    public static T ParseProfile<T>(JsonElement root, Action<T, string> validate, params string[] envelope)
    {
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(envelope);

        var profile = Deserialize<T>(root, envelope) ?? throw new JsonException("The profile is empty.");

        validate(profile, "The profile");

        return profile;
    }

    /// <summary>
    ///     Enumerates the documents an assembly embeds under a folder, each called the way its file is,
    ///     without the extension.
    /// </summary>
    /// <param name="assembly">The assembly that contains the documents as embedded resources.</param>
    /// <param name="folder">The folder they are embedded from, such as <c>"Options"</c>.</param>
    /// <returns>
    ///     The documents, name and contents.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="assembly"/> or <paramref name="folder"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     A document could not be read.
    /// </exception>
    public static IEnumerable<(string Name, string Contents)> EnumerateEmbeddedProfiles(Assembly assembly, string folder)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(folder);

        return EnumerateEmbedded(assembly, $".{folder.Replace('/', '.')}.");
    }

    /// <summary>
    ///     Copies every leaf <paramref name="overrides"/> has a value for onto <paramref name="target"/>. A null leaf
    ///     keeps the standing value. A section recurses.
    /// </summary>
    /// <param name="target">The object to copy onto.</param>
    /// <param name="overrides">The object whose non-null leaves win.</param>
    /// <exception cref="ArgumentNullException">
    ///     Any argument is <see langword="null"/>.
    /// </exception>
    public static void ApplyOverrides(object target, object overrides)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(overrides);

        foreach (var member in OptionsContract.GetMembers(target.GetType()))
        {
            if (!member.CanGet)
            {
                continue;
            }

            if (member.IsSection)
            {
                if (member.Get(target) is { } into && member.Get(overrides) is { } from)
                {
                    ApplyOverrides(into, from);
                }

                continue;
            }

            if (member.CanSet && member.Get(overrides) is { } value)
            {
                member.Set(target, value);
            }
        }
    }

    private static IEnumerable<(string Name, string Contents)> EnumerateEmbedded(Assembly assembly, string prefix)
    {
        foreach (string resource in assembly.GetManifestResourceNames())
        {
            // The folder can contain resources that aren't documents.
            if (!resource.EndsWith(".jsonc", StringComparison.Ordinal))
            {
                continue;
            }

            int start = resource.IndexOf(prefix, StringComparison.Ordinal);
            if (start < 0)
            {
                continue;
            }

            string name = resource[(start + prefix.Length)..^".jsonc".Length];

            yield return (name, ReadResource(assembly, resource));
        }
    }

    private static string ReadResource(Assembly assembly, string resource)
    {
        using var stream = assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"The document '{resource}' could not be read.");
        using StreamReader reader = new(stream);

        return reader.ReadToEnd();
    }

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new HexNumberConverter() },
        };

        // Makes the options read-only so no consumer can change the shared grammar, filling in the resolver the
        // serializer requires before it freezes.
        options.MakeReadOnly(populateMissingResolver: true);

        return options;
    }
}
