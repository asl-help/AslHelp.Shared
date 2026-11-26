using AslHelp.Options;
using System;
using System.Text.Json;

namespace AslHelp.Options.Binding;

/// <summary>
///     Provides the grammar every options document is read with. Binds documents to options types and applies one
///     bound object's overrides onto another.
/// </summary>
public static class OptionsBinder
{
    // Share the grammar between a parse and a bind, so both agree on comments and trailing commas.
    private const bool AllowTrailingCommas = true;
    private const JsonCommentHandling CommentHandling = JsonCommentHandling.Skip;

    private static readonly JsonDocumentOptions _jsonc = new()
    {
        AllowTrailingCommas = AllowTrailingCommas,
        CommentHandling = CommentHandling,
    };

    /// <summary>
    ///     Gets the serializer options every document binds with: JSONC, keys in the camel case of an options type's own
    ///     member identifiers and whole numbers read as JSON numbers or <c>"0x"</c> hex strings.
    /// </summary>
    /// <remarks>
    ///     A schema is written with the same options, so the schema and the binder spell keys the same way.
    /// </remarks>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

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

        OptionsValidator.ValidateKeys(root, typeof(T), Options, SR.DocumentObjectName, envelope);

        return root.Deserialize<T>(Options);
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

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new()
        {
            ReadCommentHandling = CommentHandling,
            AllowTrailingCommas = AllowTrailingCommas,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new HexNumberConverter() },
        };

        // Makes the options read-only so no consumer can change the shared grammar, filling in the resolver the
        // serializer requires before it freezes.
        options.MakeReadOnly(populateMissingResolver: true);

        return options;
    }
}
