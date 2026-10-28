using AslHelp.Options;
using AslHelp.Options.Capabilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AslHelp.Options.Binding;

/// <summary>
///     Provides the contract of an options type: the members the serializer binds, each under its document key. A
///     member whose type is marked <c>[Options]</c> nests as a section. Everything else is a leaf.
/// </summary>
/// <remarks>
///     The members come from the serializer's own metadata, so a field opted in with <c>[JsonInclude]</c>, a member
///     left out with <c>[JsonIgnore]</c>, a renamed key and an inherited member are enumerated exactly as they bind.
/// </remarks>
public static class OptionsContract
{
    private static readonly ConcurrentDictionary<Type, OptionMember[]> _members = new();

    /// <summary>
    ///     Lists the members the serializer binds on an options type, each under its document key.
    /// </summary>
    /// <remarks>
    ///     A <c>[JsonIgnore]</c> member stays in the serializer's metadata with neither accessor, so its key reads as
    ///     known and its value goes nowhere. This method leaves that member out.
    /// </remarks>
    /// <param name="type">The options type.</param>
    /// <returns>
    ///     The members, in the order the serializer binds them.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    public static IReadOnlyList<OptionMember> GetMembers(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return _members.GetOrAdd(type, static type => CreateMembers(OptionsBinder.Options.GetTypeInfo(type)));
    }

    /// <summary>
    ///     Finds the member an options type binds under a document key.
    /// </summary>
    /// <param name="type">The options type.</param>
    /// <param name="key">The key.</param>
    /// <param name="member">
    ///     The member. <see langword="null"/> when the type binds nothing under the key.
    /// </param>
    /// <returns>
    ///     <see langword="true"/> when the type binds a member under the key, otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="type"/> or <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    public static bool TryGetMember(Type type, string key, [NotNullWhen(true)] out OptionMember? member)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(key);

        foreach (var candidate in GetMembers(type))
        {
            if (candidate.Key == key)
            {
                member = candidate;
                return true;
            }
        }

        member = null;
        return false;
    }

    /// <summary>
    ///     Finds the member an options type binds under a document path, descending its sections.
    /// </summary>
    /// <remarks>
    ///     The keys are passed 1 by 1 rather than joined because a key may itself contain a <c>.</c>.
    /// </remarks>
    /// <param name="type">The options type.</param>
    /// <param name="keys">The keys from the type down to the member, at least one.</param>
    /// <returns>
    ///     The member.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="type"/> or <paramref name="keys"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="keys"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     A key along the path is none the type there binds.
    /// </exception>
    public static OptionMember GetMember(Type type, params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Length == 0)
        {
            throw new ArgumentException("A member is found by at least one key.", nameof(keys));
        }

        OptionMember? member = null;

        foreach (string key in keys)
        {
            if (member is not null)
            {
                type = GetUnderlyingType(member.Type);
            }

            if (!TryGetMember(type, key, out member))
            {
                throw new InvalidOperationException($"{type.Name} has no member the document calls '{key}'.");
            }
        }

        return member!;
    }

    /// <summary>
    ///     Reads the value an options object has under a document path, descending its sections.
    /// </summary>
    /// <param name="options">The object.</param>
    /// <param name="keys">The keys from the object down to the member, at least one.</param>
    /// <returns>
    ///     The value. <see langword="null"/> when the member has no value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="options"/> or <paramref name="keys"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="keys"/> is empty.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     A key along the path is none the type there binds or a section along the path is empty.
    /// </exception>
    public static object? GetValue(object options, params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(keys);

        if (keys.Length == 0)
        {
            throw new ArgumentException("A value is read by at least one key.", nameof(keys));
        }

        object holder = options;

        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (!TryGetMember(holder.GetType(), keys[i], out var section))
            {
                throw new InvalidOperationException($"{holder.GetType().Name} has no member the document calls '{keys[i]}'.");
            }

            holder = section.Get(holder)
                ?? throw new InvalidOperationException($"'{string.Join(".", keys, 0, i + 1)}' holds no section.");
        }

        if (!TryGetMember(holder.GetType(), keys[^1], out var member))
        {
            throw new InvalidOperationException($"{holder.GetType().Name} has no member the document calls '{keys[^1]}'.");
        }

        return member.Get(holder);
    }

    /// <summary>
    ///     Lists the capabilities an options type marks on its members and the members of its sections.
    /// </summary>
    /// <param name="type">The options type.</param>
    /// <returns>
    ///     The capabilities' keys, each once, in the order the members declare them.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="type"/> is <see langword="null"/>.
    /// </exception>
    public static IReadOnlyList<string> GetCapabilities(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        List<string> found = [];
        CollectCapabilities(type, found);

        return found;
    }

    private static OptionValidationAttribute? GetValidator(JsonPropertyInfo property)
    {
        return property.AttributeProvider?.GetCustomAttributes(typeof(OptionValidationAttribute), inherit: false)
            .OfType<OptionValidationAttribute>()
            .FirstOrDefault();
    }

    private static void CollectCapabilities(Type type, List<string> found)
    {
        foreach (var member in GetMembers(type))
        {
            foreach (string capability in member.Capabilities)
            {
                if (!found.Contains(capability))
                {
                    found.Add(capability);
                }
            }

            if (member.IsSection)
            {
                CollectCapabilities(GetUnderlyingType(member.Type), found);
            }
        }
    }

    private static OptionMember[] CreateMembers(JsonTypeInfo info)
    {
        return [.. GetBoundProperties(info).Select(static property => new OptionMember(
            property,
            IsDeclaredRequired(property),
            GetCapabilities(property),
            IsOptions(GetUnderlyingType(property.PropertyType)),
            property.AttributeProvider?.GetCustomAttributes(typeof(RequiredWhenAttribute), inherit: false)
                .OfType<RequiredWhenAttribute>()
                .FirstOrDefault(),
            GetValidator(property)))];
    }

    private static string[] GetCapabilities(JsonPropertyInfo member)
    {
        return member.AttributeProvider?.GetCustomAttributes(typeof(CapabilityAttribute), inherit: false)
            .OfType<CapabilityAttribute>()
            .Select(static marked => marked.Name)
            .ToArray() ?? [];
    }

    internal static IEnumerable<JsonPropertyInfo> GetBoundProperties(JsonTypeInfo info)
    {
        return info.Properties.Where(static member => member.Get is not null || member.Set is not null);
    }

    // Reads the C# `required` mark rather than the serializer's own flag, which a `[SetsRequiredMembers]` constructor
    // clears so a sparse override binds. The check runs after the override is merged, on the whole graph. Compares the
    // mark by name, since each assembly has its own polyfilled copy of it.
    private static bool IsDeclaredRequired(JsonPropertyInfo member)
    {
        const string RequiredAttribute = "System.Runtime.CompilerServices.RequiredMemberAttribute";

        foreach (var attribute in (member.AttributeProvider?.GetCustomAttributes(inherit: false) ?? []).Cast<Attribute>())
        {
            if (attribute.GetType().FullName == RequiredAttribute)
            {
                return true;
            }
        }

        return false;
    }

    private static Type GetUnderlyingType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static bool IsOptions(Type type)
    {
        return type.IsDefined(typeof(OptionsAttribute), inherit: false);
    }
}
