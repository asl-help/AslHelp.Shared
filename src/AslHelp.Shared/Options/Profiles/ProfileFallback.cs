using System;
using System.Text.Json.Serialization;

namespace AslHelp.Options.Profiles;

/// <summary>
///     Represents an interval of versions without an entry of their own. A version in the interval selects a nearby
///     entry whose data was measured on <see cref="SourceVersion"/>, with a warning that the data may be incorrect.
/// </summary>
/// <param name="From">The first version of the interval.</param>
/// <param name="Until">The version the interval stops before.</param>
/// <param name="SourceVersion">
///     The version the selected entry's data was measured on, written into the warning. A fallback document writes it
///     under <c>usingDataFrom</c>.
/// </param>
public sealed record ProfileFallback(
    string From,
    string Until,
    [property: JsonPropertyName("usingDataFrom")] string SourceVersion)
{
    /// <summary>
    ///     Gets a value indicating whether <paramref name="version"/> lies in the interval.
    /// </summary>
    /// <param name="version">A complete version, such as <c>"2023.1.2f1"</c>.</param>
    /// <returns>
    ///     <see langword="true"/> if <paramref name="version"/> is at or above <see cref="From"/> and below
    ///     <see cref="Until"/>, otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="version"/> is <see langword="null"/>.
    /// </exception>
    public bool Contains(string version)
    {
        ArgumentNullException.ThrowIfNull(version);

        int[] components = ProfileVersions.GetComponents(version);

        return ProfileVersions.Compare(ProfileVersions.GetComponents(From), components) <= 0
            && ProfileVersions.Compare(components, ProfileVersions.GetComponents(Until)) < 0;
    }
}
