using AslHelp.Options;
using AslHelp.Options.Binding;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace AslHelp.Options.Schema;

/// <summary>
///     Provides the JSON schema of an options type, exported from the serializer's own contract for it.
/// </summary>
/// <remarks>
///     The serializer decides which keys exist and what each binds, so the schema and the binder never disagree about
///     membership. The schema adds only what the serializer's contract lacks: a hex number's 2 spellings, a choice's
///     spellings, a type or converter that writes its own schema, a type's own rules and the colon spelling an options
///     file allows.
/// </remarks>
public static class OptionsSchemaExporter
{
    private const string Draft = "https://json-schema.org/draft/2020-12/schema";

    // A key of 2 or more keys joined by colons, which the loader expands into nested sections.
    private const string ColonKey = "^[^:]+(:[^:]+)+$";

    private static readonly JsonSerializerOptions _strict = Strict();

    // A schema is a repo file read by people and contains no untrusted text, so nothing is escaped past what JSON
    // demands.
    private static readonly JsonWriterOptions _minified = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    ///     Exports the schema of a document that binds to <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The options type the document binds to.</param>
    /// <param name="title">The schema's title.</param>
    /// <param name="colonKeys">Whether a <c>section:key</c> spelling may set a nested option.</param>
    /// <returns>
    ///     The schema, opening with the draft it is written against and its title.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="type"/> or <paramref name="title"/> is <see langword="null"/>.
    /// </exception>
    public static JsonObject Export(Type type, string title, bool colonKeys)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(title);

        JsonSchemaExporterOptions exporter = new()
        {
            TreatNullObliviousAsNonNullable = true,
            TransformSchemaNode = (context, node) => Transform(context, node, colonKeys),
        };

        var exported = JsonSchemaExporter.GetJsonSchemaAsNode(_strict, type, exporter).AsObject();

        JsonObject schema = new()
        {
            ["$schema"] = Draft,
            ["title"] = title,
        };

        MoveInto(exported, schema);
        AllowSchemaPointer(schema);

        return schema;
    }

    // Accepts the `$schema` key at the root, because a document points its editor at the schema with it and the
    // binder takes it as an envelope key.
    private static void AllowSchemaPointer(JsonObject schema)
    {
        if (schema["properties"] is not JsonObject properties)
        {
            return;
        }

        JsonObject withPointer = new() { ["$schema"] = new JsonObject { ["type"] = "string" } };

        foreach (var (key, value) in properties.ToArray())
        {
            _ = properties.Remove(key);
            withPointer[key] = value;
        }

        schema["properties"] = withPointer;
    }

    /// <summary>
    ///     Writes a schema the way the repository keeps one: minified, with nothing after the closing brace.
    /// </summary>
    /// <param name="schema">The schema.</param>
    /// <returns>
    ///     The schema's JSON.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="schema"/> is <see langword="null"/>.
    /// </exception>
    public static string Minified(JsonObject schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, _minified))
        {
            schema.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    // Binds the way a document does, refusing a key no member takes, so the exported objects close the same way.
    private static JsonSerializerOptions Strict()
    {
        JsonSerializerOptions options = new(OptionsBinder.Options)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };

        options.MakeReadOnly(populateMissingResolver: true);

        return options;
    }

    private static JsonNode Transform(JsonSchemaExporterContext context, JsonNode node, bool colonKeys)
    {
        var type = Nullable.GetUnderlyingType(context.TypeInfo.Type) ?? context.TypeInfo.Type;

        if (context.PropertyInfo?.AttributeProvider?.GetCustomAttributes(typeof(OptionCheckAttribute), inherit: false)
                .FirstOrDefault() is OptionCheckAttribute check)
        {
            return Written(check.WriteSchema);
        }

        if (type == typeof(HexNumber))
        {
            return HexNumbers();
        }

        if (DescribedBy(context.PropertyInfo, type) is { } describe)
        {
            return Written(describe);
        }

        // A reference points at an object already written, which has everything below it.
        if (node is not JsonObject schema || schema.ContainsKey("$ref"))
        {
            return node;
        }

        // A list or a map of whole numbers binds each one through the hex converter, which the exporter doesn't read.
        if (context.TypeInfo.Kind is JsonTypeInfoKind.Enumerable or JsonTypeInfoKind.Dictionary
            && context.TypeInfo.ElementType is { } element
            && _strict.GetConverter(Nullable.GetUnderlyingType(element) ?? element) is HexNumberConverter)
        {
            schema[context.TypeInfo.Kind is JsonTypeInfoKind.Enumerable ? "items" : "additionalProperties"] = HexNumbers();

            return schema;
        }

        if (context.TypeInfo.Kind is not JsonTypeInfoKind.Object)
        {
            return node;
        }

        // An object with an extension data member takes any key, as the binder does. The object's own validation
        // checks the values that go there.
        if (context.TypeInfo.Properties.Any(static member => member.IsExtensionData))
        {
            schema["additionalProperties"] = true;
        }

        // The exporter writes a member bound through a custom converter as unconstrained, without passing it here, so a
        // whole number taking the hex converter of the shared grammar is written from its object instead.
        if (schema["properties"] is JsonObject properties)
        {
            foreach (var member in context.TypeInfo.Properties)
            {
                var memberType = Nullable.GetUnderlyingType(member.PropertyType) ?? member.PropertyType;

                if (properties.ContainsKey(member.Name) && (member.CustomConverter ?? _strict.GetConverter(memberType)) is HexNumberConverter)
                {
                    properties[member.Name] = HexNumbers();
                }
            }
        }

        if (colonKeys && context.TypeInfo.Properties.Count > 0)
        {
            schema["patternProperties"] = new JsonObject { [ColonKey] = true };
        }

        if (StaticWriter(type, "WriteRules") is { } rules)
        {
            var written = Written(writer =>
            {
                writer.WriteStartObject();
                rules(writer);
                writer.WriteEndObject();
            }).AsObject();

            MoveInto(written, schema);
        }

        return schema;
    }

    // Writes a number as either a JSON number or hex text, the 2 spellings the converter accepts.
    private static JsonObject HexNumbers()
    {
        return new JsonObject
        {
            ["anyOf"] = new JsonArray(
                new JsonObject { ["type"] = "integer" },
                new JsonObject { ["type"] = "string", ["pattern"] = "^0x[a-fA-F0-9]+$" }),
        };
    }

    // Finds the schema writer of a value that describes itself: the type's own writer or the writer of the converter
    // that the member or the type binds through.
    private static Action<Utf8JsonWriter>? DescribedBy(JsonPropertyInfo? property, Type type)
    {
        var converter = property?.AttributeProvider?.GetCustomAttributes(typeof(JsonConverterAttribute), inherit: false)
            .OfType<JsonConverterAttribute>()
            .FirstOrDefault()?.ConverterType
            ?? type.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType;

        return StaticWriter(type, "WriteSchema") ?? (converter is null ? null : StaticWriter(converter, "WriteSchema"));
    }

    private static Action<Utf8JsonWriter>? StaticWriter(Type type, string name)
    {
        var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, [typeof(Utf8JsonWriter)], null);

        return method is null ? null : (Action<Utf8JsonWriter>)method.CreateDelegate(typeof(Action<Utf8JsonWriter>));
    }

    // Moves every key of one object into another, since a node belongs to one parent at a time.
    private static void MoveInto(JsonObject from, JsonObject into)
    {
        foreach (var pair in from.ToList())
        {
            _ = from.Remove(pair.Key);
            into[pair.Key] = pair.Value;
        }
    }

    private static JsonNode Written(Action<Utf8JsonWriter> write)
    {
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream))
        {
            write(writer);
        }

        return JsonNode.Parse(stream.ToArray())!;
    }
}
