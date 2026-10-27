using AslHelp.Options;
using AslHelp.Options.Capabilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AslHelp.Options.Binding;

/// <summary>
///     Provides checks of an options object graph against its contract: required leaves, marked values and document
///     keys.
/// </summary>
public static class OptionsValidator
{
    // Maps each object type to its members by key, whether the type takes unknown keys and the keys a refusal lists. A
    // weak table lets the entries go with the options a caller made, however many the caller makes.
    private static readonly ConditionalWeakTable<JsonTypeInfo, ObjectKeys> _keys = new();

    /// <summary>
    ///     Checks that every required leaf has a value. A leaf is required when it is declared
    ///     <see langword="required"/> or marked <see cref="RequiredWhenAttribute"/> with its deciding option set to 1 of
    ///     the mark's values. The error gives the origin and full option path of a leaf without a value.
    /// </summary>
    /// <param name="options">The object to check.</param>
    /// <param name="origin">Where the values came from, written into the error.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    ///     A required leaf has no value.
    /// </exception>
    public static void ThrowIfMissingRequired(object options, string origin)
    {
        ArgumentNullException.ThrowIfNull(options);

        ThrowIfMissingRequired(options, origin, "", options);
    }

    /// <summary>
    ///     Checks that every required leaf has a value, passing over the leaves of every capability the options declare
    ///     unavailable. A leaf is required when it is declared <see langword="required"/> or marked
    ///     <see cref="RequiredWhenAttribute"/> with its deciding option set to 1 of the mark's values.
    /// </summary>
    /// <param name="options">The object to check.</param>
    /// <param name="origin">Where the values came from, written into the error.</param>
    /// <param name="unavailable">The capabilities the options declare they cannot serve, by key.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    ///     A required leaf has no value. A declared capability is none the options mark. A declaration has no state,
    ///     has a state that is not a <see cref="CapabilityState"/> spelling or has no reason.
    /// </exception>
    public static void ThrowIfMissingRequired(object options, string origin, IReadOnlyDictionary<string, UnavailableCapability>? unavailable)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (unavailable is null || unavailable.Count == 0)
        {
            ThrowIfMissingRequired(options, origin, "", options);

            return;
        }

        var known = OptionsContract.GetCapabilities(options.GetType());

        foreach (var pair in unavailable)
        {
            string capability = pair.Key;

            if (pair.Value is not { } declared)
            {
                throw new JsonException($"{origin} declares 'unavailable.{capability}' without a state or a reason.");
            }

            if (!known.Contains(capability))
            {
                throw new JsonException(
                    $"{origin} declares '{capability}' unavailable, which is no capability it has. Use {string.Join(", ", known)}.");
            }

            ThrowIfMissingRequired(declared, origin, $"unavailable.{capability}", declared);
            _ = Choice.Parse<CapabilityState>(declared.State!, origin, $"unavailable.{capability}.state");
        }

        ThrowIfMissingRequired(options, origin, "", options, [.. unavailable.Keys]);
    }

    /// <summary>
    ///     Lists every required leaf that has no value. A leaf is required when it is declared
    ///     <see langword="required"/> or marked <see cref="RequiredWhenAttribute"/> with its deciding option set to 1 of
    ///     the mark's values.
    /// </summary>
    /// <param name="options">The object to check.</param>
    /// <returns>
    ///     Each leaf's path from the root, its keys joined by dots, in the order the types declare them.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    public static IReadOnlyList<string> GetUnsetKeys(object options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> unset = [];
        CollectUnsetKeys(options, "", options, unavailable: null, unset);

        return unset;
    }

    /// <summary>
    ///     Checks that every key in <paramref name="element"/> is one the serializer binds into
    ///     <paramref name="type"/>. The error for an unknown key lists the keys that exist.
    /// </summary>
    /// <remarks>
    ///     Every object the serializer binds is checked by its own type, down through list items and dictionary values.
    ///     A value a converter binds is left to the converter. An object with an extension data member takes any key,
    ///     as the serializer does. The type's own validation checks the values that go there.
    /// </remarks>
    /// <param name="element">The document or the part of the document the type binds.</param>
    /// <param name="type">The type the document binds to.</param>
    /// <param name="options">The serializer options the document binds through.</param>
    /// <param name="objectName">
    ///     The name of the object, written into the error. A nested object goes by the key it sits under.
    /// </param>
    /// <param name="envelope">The keys the document adds around the type at its root, such as <c>$schema</c>.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="type"/>, <paramref name="options"/>, <paramref name="objectName"/> or
    ///     <paramref name="envelope"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    ///     A key is no member the type binds.
    /// </exception>
    public static void ValidateKeys(
        JsonElement element,
        Type type,
        JsonSerializerOptions options,
        string objectName,
        params string[] envelope)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(objectName);
        ArgumentNullException.ThrowIfNull(envelope);

        Validate(element, options.GetTypeInfo(type), options, objectName, envelope);
    }

    /// <summary>
    ///     Checks every option marked with an <see cref="OptionValidationAttribute"/> that has a value, including the
    ///     options of every section. The error gives the origin and full option path of a value that does not fit.
    /// </summary>
    /// <remarks>
    ///     An option left unset passes, since the reader decides what an unset option means, so a sparse set of
    ///     overrides is checked as readily as a whole document.
    /// </remarks>
    /// <param name="options">The object to check.</param>
    /// <param name="origin">Where the values came from, written into the error.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="JsonException">
    ///     A marked option has a value its mark refuses.
    /// </exception>
    public static void ValidateValues(object options, string origin)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidateValues(options, origin, "");
    }

    private static void Validate(JsonElement element, JsonTypeInfo info, JsonSerializerOptions options, string objectName, string[] envelope)
    {
        // Leaves a value of another shape for the serializer to refuse. A value the serializer reads whole or through a
        // converter has no keys to check.
        if (info.Kind is JsonTypeInfoKind.Object && element.ValueKind is JsonValueKind.Object)
        {
            ValidateObject(element, info, options, objectName, envelope);
        }
        else if (info.Kind is JsonTypeInfoKind.Enumerable && element.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                Validate(item, options.GetTypeInfo(info.ElementType!), options, objectName, []);
            }
        }
        else if (info.Kind is JsonTypeInfoKind.Dictionary && element.ValueKind is JsonValueKind.Object)
        {
            foreach (var entry in element.EnumerateObject())
            {
                Validate(entry.Value, options.GetTypeInfo(info.ElementType!), options, entry.Name, []);
            }
        }
    }

    private static void ValidateObject(JsonElement element, JsonTypeInfo info, JsonSerializerOptions options, string objectName, string[] envelope)
    {
        var (byName, isOpen, displayList) = _keys.GetValue(info, static info => GetKeys(info));

        foreach (var property in element.EnumerateObject())
        {
            if (byName.TryGetValue(property.Name, out var member))
            {
                // A member with a converter of its own binds whatever the converter takes.
                if (member.CustomConverter is null)
                {
                    Validate(property.Value, options.GetTypeInfo(member.PropertyType), options, property.Name, []);
                }

                continue;
            }

            if (!isOpen && Array.IndexOf(envelope, property.Name) < 0)
            {
                throw new JsonException($"The '{objectName}' object has an unknown option '{property.Name}'. Use {displayList}.");
            }
        }
    }

    private static ObjectKeys GetKeys(JsonTypeInfo info)
    {
        var members = OptionsContract.GetBoundProperties(info).ToList();
        Dictionary<string, JsonPropertyInfo> named = [with(StringComparer.Ordinal)];

        foreach (var member in members)
        {
            if (!member.IsExtensionData && !named.ContainsKey(member.Name))
            {
                named[member.Name] = member;
            }
        }

        return new(named, members.Any(static member => member.IsExtensionData), string.Join(", ", named.Keys));
    }

    private static void ThrowIfMissingRequired(object options, string origin, string path, object root, ICollection<string>? unavailable = null)
    {
        List<string> unset = [];
        CollectUnsetKeys(options, path, root, unavailable, unset);

        if (unset.Count > 0)
        {
            throw new JsonException($"{origin} declares no '{unset[0]}'.");
        }
    }

    private static void CollectUnsetKeys(object options, string path, object root, ICollection<string>? unavailable, List<string> unset)
    {
        foreach (var member in OptionsContract.GetMembers(options.GetType()))
        {
            string at = path.Length == 0 ? member.Key : $"{path}.{member.Key}";

            if (!member.CanGet || (unavailable is not null && member.Capabilities is { Count: > 0 } marked && marked.All(unavailable.Contains)))
            {
                continue;
            }

            if (member.IsSection)
            {
                if (member.Get(options) is { } section)
                {
                    CollectUnsetKeys(section, at, root, unavailable, unset);
                }

                continue;
            }

            if ((member.IsRequired || IsRequiredBy(root, member.RequiredWhen)) && member.Get(options) is null)
            {
                unset.Add(at);
            }
        }
    }

    // Checks whether the deciding option of a conditional mark has 1 of the values that make the member required.
    private static bool IsRequiredBy(object root, RequiredWhenAttribute? when)
    {
        return when is not null
            && OptionsContract.GetValue(root, when.Option.Split('.')) is string value
            && when.Values.Any(required => string.Equals(required, value, StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateValues(object options, string origin, string path)
    {
        foreach (var member in OptionsContract.GetMembers(options.GetType()))
        {
            string at = path.Length == 0 ? member.Key : $"{path}.{member.Key}";

            if (!member.CanGet)
            {
                continue;
            }

            if (member.IsSection)
            {
                if (member.Get(options) is { } section)
                {
                    ValidateValues(section, origin, at);
                }

                continue;
            }

            if (member.Validator is { } validator && member.Get(options) is { } value)
            {
                validator.Validate(value, origin, at);
            }
        }
    }

    private sealed record ObjectKeys(
        Dictionary<string, JsonPropertyInfo> ByName,
        bool IsOpen,
        string DisplayList);
}
