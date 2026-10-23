using System;

namespace AslHelp.Options.Profiles;

/// <summary>
///     Represents the key of an entry in a <see cref="VersionedProfileCatalogue{TOptions}"/>: the partition, the
///     version the entry applies from and the pointer size its offsets are laid out for.
/// </summary>
/// <remarks>
///     2 keys are equal when their partitions and versions match in any case and their pointer sizes match, so an
///     entry registered under 1 spelling is found under another.
/// </remarks>
/// <param name="Partition">
///     The part in front of the version that splits the catalogue's entries. Empty for a catalogue without
///     partitions.
/// </param>
/// <param name="Version">The version the entry applies from.</param>
/// <param name="PointerSize">The pointer size the entry's offsets are laid out for, 4 or 8.</param>
public readonly record struct ProfileKey(
    string Partition,
    string Version,
    int PointerSize)
{
    /// <summary>
    ///     Compares the partition and version in any case and the pointer size exactly.
    /// </summary>
    /// <param name="other">The key to compare against.</param>
    /// <returns>
    ///     <see langword="true"/> if the 2 keys identify the same entry, otherwise <see langword="false"/>.
    /// </returns>
    public bool Equals(ProfileKey other)
    {
        return PointerSize == other.PointerSize
            && string.Equals(Partition, other.Partition, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Version, other.Version, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // Hashes with the comparer that equality uses. The empty fallback covers the null strings of a default key.
        return HashCode.Combine(
            PointerSize,
            StringComparer.OrdinalIgnoreCase.GetHashCode(Partition ?? ""),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Version ?? ""));
    }

    /// <summary>
    ///     Gets the lane of a pointer size as an entry's file name writes it.
    /// </summary>
    /// <param name="pointerSize">The pointer size, 4 or 8.</param>
    /// <returns>
    ///     <c>"x64"</c> for 8, otherwise <c>"x86"</c>.
    /// </returns>
    public static string LaneOf(int pointerSize)
    {
        return pointerSize == 8 ? "x64" : "x86";
    }

    /// <summary>
    ///     Reads an entry's key from the name of its shipped file,
    ///     <c>[&lt;partition&gt;-]&lt;version&gt;-&lt;x86|x64&gt;</c>.
    /// </summary>
    /// <param name="name">The file's name without its extension.</param>
    /// <param name="partitioned">Whether the name starts with a partition.</param>
    /// <param name="key">The key when the name has the expected shape.</param>
    /// <returns>
    ///     <see langword="true"/> if the name has the expected shape, otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public static bool TryParseShippedName(string name, bool partitioned, out ProfileKey key)
    {
        ArgumentNullException.ThrowIfNull(name);

        key = default;

        int arch = name.LastIndexOf('-');
        int dash = partitioned ? name.IndexOf('-') : -1;

        if (arch <= dash + 1 || (partitioned && dash <= 0))
        {
            return false;
        }

        int pointerSize = name[(arch + 1)..] switch { "x64" => 8, "x86" => 4, _ => 0 };

        if (pointerSize == 0)
        {
            return false;
        }

        key = new ProfileKey(partitioned ? name[..dash] : "", name[(dash + 1)..arch], pointerSize);

        return true;
    }
}
