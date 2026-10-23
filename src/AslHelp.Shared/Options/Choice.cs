using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AslHelp.Options;

/// <summary>
///     Parses a choice option: a string whose accepted spellings are the kebab case of an enum's member names, matched
///     ignoring case, plus whatever a member's own <see cref="ChoiceAliasAttribute"/> adds.
/// </summary>
/// <remarks>
///     Throws a <see cref="JsonException"/> that lists every valid spelling for a value no spelling matches, so every
///     options file reports a wrong choice the same way.
/// </remarks>
public static class Choice
{
    private static readonly ConcurrentDictionary<Type, (object Member, string Alias)[]> _aliases = new();

    /// <summary>Parses <paramref name="value"/> as a <typeparamref name="TEnum"/> spelling.</summary>
    /// <typeparam name="TEnum">The enum whose members are the accepted choices.</typeparam>
    /// <param name="value">The spelling a source set.</param>
    /// <param name="origin">Where the value came from, written into the error.</param>
    /// <param name="option">The option's name, written into the error.</param>
    /// <returns>
    ///     The chosen member.
    /// </returns>
    /// <exception cref="JsonException">
    ///     <paramref name="value"/> is none of the spellings.
    /// </exception>
    public static TEnum Parse<TEnum>(string value, string origin, string option)
        where TEnum : struct, Enum
    {
        return (TEnum)Parse(typeof(TEnum), value, origin, option);
    }

    /// <summary>
    ///     Lists the spellings <see cref="Parse{TEnum}(string, string, string)"/> accepts, in the enum's own order.
    /// </summary>
    /// <typeparam name="TEnum">The enum whose members are the accepted choices.</typeparam>
    /// <returns>
    ///     Each member's name in kebab case.
    /// </returns>
    public static string[] Spellings<TEnum>()
        where TEnum : struct, Enum
    {
        return Spellings(typeof(TEnum));
    }

    /// <summary>
    ///     Gets the extra spellings the enum's members' own <see cref="ChoiceAliasAttribute"/> add, in the enum's own
    ///     order, cached per <typeparamref name="TEnum"/>.
    /// </summary>
    /// <typeparam name="TEnum">The enum whose members are the accepted choices.</typeparam>
    /// <returns>
    ///     Each member's registered alias, if it has one.
    /// </returns>
    public static string[] Aliases<TEnum>()
        where TEnum : struct, Enum
    {
        return Aliases(typeof(TEnum));
    }

    // Parses a spelling of an enum known only at run time, as a marked option is checked.
    internal static object Parse(Type enumType, string value, string origin, string option)
    {
        ArgumentNullException.ThrowIfNull(value);

        foreach (object member in Enum.GetValues(enumType))
        {
            if (string.Equals(value, Kebab(member.ToString()), StringComparison.OrdinalIgnoreCase))
            {
                return member;
            }
        }

        foreach (var (member, alias) in Aliased(enumType))
        {
            if (string.Equals(value, alias, StringComparison.OrdinalIgnoreCase))
            {
                return member;
            }
        }

        throw new JsonException(
            $"{origin} declares the unknown '{option}' value '{value}', expected {Listed(enumType)}.");
    }

    // Lists the spellings of an enum known only at run time, as a schema writes them.
    internal static string[] Spellings(Type enumType)
    {
        string[] names = Enum.GetNames(enumType);

        for (int i = 0; i < names.Length; i++)
        {
            names[i] = Kebab(names[i]);
        }

        return names;
    }

    // Lists the aliases of an enum known only at run time, as a schema writes them.
    internal static string[] Aliases(Type enumType)
    {
        return [.. Aliased(enumType).Select(static a => a.Alias)];
    }

    private static string Listed(Type enumType)
    {
        string[] spellings = [.. Spellings(enumType), .. Aliases(enumType)];

        StringBuilder listed = new();

        for (int i = 0; i < spellings.Length; i++)
        {
            if (i > 0)
            {
                listed.Append(i == spellings.Length - 1 ? " or " : ", ");
            }

            listed.Append('"').Append(spellings[i]).Append('"');
        }

        return listed.ToString();
    }

    private static (object Member, string Alias)[] Aliased(Type enumType)
    {
        return _aliases.GetOrAdd(enumType, static t =>
            [.. t.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(static f => (Member: f.GetValue(null)!, Alias: f.GetCustomAttribute<ChoiceAliasAttribute>()?.Spelling))
                .Where(static a => a.Alias is not null)
                .Select(static a => (a.Member, a.Alias!))]);
    }

    // A word starts at a capital that follows a lowercase letter, so a member such as FField or UProperty keeps its
    // capitals together, which is how the engine spells the type.
    private static string Kebab(string name)
    {
        StringBuilder split = new();

        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0 && char.IsLower(name[i - 1]))
            {
                split.Append('-');
            }

            split.Append(char.ToLowerInvariant(name[i]));
        }

        return split.ToString();
    }
}
