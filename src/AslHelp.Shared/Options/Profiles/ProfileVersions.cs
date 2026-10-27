using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AslHelp.Options.Profiles;

/// <summary>
///     Provides the ordering of the entries of a version-keyed catalogue. A version reads as numbers separated by
///     dots. An entry applies from its own version onward, so a lookup takes the newest entry at or below the target.
/// </summary>
/// <remarks>
///     A Unity revision such as <c>0f4</c> has a release type and an increment after its number. Both take part in the
///     order because a layout can change between <c>6000.7.0a2</c> and <c>6000.7.0a5</c>.
/// </remarks>
public static class ProfileVersions
{
    /// <summary>
    ///     Matches the start of an engine version, which is 2 to 4 numbers separated by dots, as in <c>2022.3.5f1</c>,
    ///     <c>4.24</c> and <c>3.8225</c>. Any text may follow the last number, such as a release suffix.
    /// </summary>
    public const string Pattern = "^[0-9]+(\\.[0-9]+){1,3}";

    /// <summary>
    ///     Checks that a version an options source sets starts the way an engine version does.
    /// </summary>
    /// <param name="version">The version. <see langword="null"/> when the source sets none.</param>
    /// <param name="origin">Where the value came from, written into the error.</param>
    /// <param name="path">The option's full path, written into the error.</param>
    /// <exception cref="JsonException">
    ///     <paramref name="version"/> is set and doesn't start with 2 to 4 numbers separated by dots.
    /// </exception>
    public static void Validate(string? version, string origin, string path)
    {
        if (version is not null && !Regex.IsMatch(version, Pattern, RegexOptions.CultureInvariant))
        {
            throw new JsonException(
                $"{origin} declares the '{path}' value '{version}', which is no engine version. "
                    + "Write it the way the engine spells it, such as \"2022.3.5f1\", \"4.24\" or \"3.8225\".");
        }
    }

    /// <summary>
    ///     Reads the numbers of a version, such as <c>6000.5.8f1</c> as 6000, 5, 8, the rank of <c>f</c> and 1.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Release types rank in Unity's order: <c>a</c> before <c>b</c>, <c>b</c> before <c>f</c> and <c>c</c>
    ///         (which rank the same), both before <c>p</c> and <c>p</c> before <c>x</c>.
    ///     </para>
    ///     <para>
    ///         Any text after the increment is ignored the way Unity ignores it, such as the <c>-Sunflower</c> of
    ///         <c>2019.3.0f11-Sunflower</c> or the <c>c1</c> of <c>2022.3.8f1c1</c>. A revision letter Unity doesn't
    ///         define is ignored too. The parts an entry leaves off don't take part in the comparison, so
    ///         <c>2021.3</c> applies to every patch of that release line.
    ///     </para>
    /// </remarks>
    /// <param name="version">The version.</param>
    /// <returns>
    ///     The numbers in order.
    /// </returns>
    public static int[] GetComponents(string version)
    {
        ArgumentNullException.ThrowIfNull(version);

        string[] parts = version.Split('.');
        List<int> components = [];

        foreach (string part in parts)
        {
            int position = 0;
            components.Add(ParseNumber(part, ref position));

            // Reads a release type and its increment after the last number only.
            if (components.Count != parts.Length || position == part.Length || GetReleaseRank(part[position]) is not int rank)
            {
                continue;
            }

            components.Add(rank);

            position++;
            int start = position;
            int increment = ParseNumber(part, ref position);

            if (position > start)
            {
                components.Add(increment);
            }
        }

        return [.. components];
    }

    /// <summary>
    ///     Trims a version to the 2 numbers of its release line, such as <c>6000.5</c>.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>
    ///     The release line.
    /// </returns>
    public static string GetReleaseLine(string version)
    {
        ArgumentNullException.ThrowIfNull(version);

        string[] parts = version.Split('.');

        return parts.Length <= 2 ? version : string.Join(".", parts, 0, 2);
    }

    /// <summary>
    ///     Chooses the entry that applies to a target, which is the newest entry at or below the target's version.
    /// </summary>
    /// <remarks>
    ///     A target that leaves parts off stands for its whole release line the way an entry that leaves parts off
    ///     does. <c>2017.4</c> takes the newest entry of 2017.4 and <c>2018.1.0</c> takes <c>2018.1.0f1</c>.
    /// </remarks>
    /// <typeparam name="TKey">The type of an entry's key.</typeparam>
    /// <param name="keys">The candidate keys, already narrowed to the target's pointer size and partition.</param>
    /// <param name="versionSelector">The function that reads the version a key applies from.</param>
    /// <param name="target">The target's version, such as <c>"6000.3.21f1"</c>.</param>
    /// <param name="key">The key that applies when an entry applies.</param>
    /// <returns>
    ///     <see langword="true"/> if an entry applies, otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Any argument is <see langword="null"/>.
    /// </exception>
    public static bool TryFindLatestAtOrBelow<TKey>(
        IEnumerable<TKey> keys,
        Func<TKey, string> versionSelector,
        string target,
        [MaybeNullWhen(false)] out TKey key)
    {
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(versionSelector);
        ArgumentNullException.ThrowIfNull(target);

        int[] wanted = GetComponents(target);
        int[]? best = null;
        key = default;

        foreach (var candidate in keys)
        {
            int[] components = GetComponents(versionSelector(candidate));

            if (!IsAtOrBelow(components, wanted))
            {
                continue;
            }

            if (best is null || Compare(components, best) > 0)
            {
                best = components;
                key = candidate;
            }
        }

        return best is not null;
    }

    /// <summary>
    ///     Compares the numbers of 1 version with the numbers of another. A number missing on either side reads as 0,
    ///     so swapping the 2 versions flips the sign.
    /// </summary>
    /// <param name="left">The first version's numbers.</param>
    /// <param name="right">The second version's numbers.</param>
    /// <returns>
    ///     A negative number, 0 or a positive number when <paramref name="left"/> is below, at or above
    ///     <paramref name="right"/>.
    /// </returns>
    public static int Compare(int[] left, int[] right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        for (int i = 0; i < Math.Max(left.Length, right.Length); i++)
        {
            int mine = i < left.Length ? left[i] : 0;
            int theirs = i < right.Length ? right[i] : 0;

            if (mine != theirs)
            {
                return mine < theirs ? -1 : 1;
            }
        }

        return 0;
    }

    // Checks whether an entry is at or below a target over the numbers both versions have, because a part either
    // version leaves off stands for the whole release line.
    private static bool IsAtOrBelow(int[] entry, int[] target)
    {
        for (int i = 0; i < entry.Length && i < target.Length; i++)
        {
            if (entry[i] != target[i])
            {
                return entry[i] < target[i];
            }
        }

        return true;
    }

    // Reads the digits at `position` as a number and moves `position` past them. Reads no digits as 0 and a run too
    // long for an int as the largest int, because a version read from a target's files can have any number of digits.
    private static int ParseNumber(string part, ref int position)
    {
        int start = position;

        while (position < part.Length && part[position] is >= '0' and <= '9')
        {
            position++;
        }

        if (position == start)
        {
            return 0;
        }

        return int.TryParse(part[start..position], NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : int.MaxValue;
    }

    // Ranks a release type in Unity's order, with `f` and `c` as the same release.
    private static int? GetReleaseRank(char kind)
    {
        return kind switch
        {
            'a' => 0,
            'b' => 1,
            'f' or 'c' => 2,
            'p' => 3,
            'x' => 4,
            _ => null,
        };
    }
}
