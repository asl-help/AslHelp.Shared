using AslHelp.Options;
using System;

namespace AslHelp.Options.Capabilities;

/// <summary>
///     Marks an option as part of a capability, a surface an entry can declare it cannot serve.
/// </summary>
/// <remarks>
///     An entry that declares the capability unavailable may leave every option marked with it unset. The required
///     check passes over those options. A section marked with it takes its members along. An option that several
///     capabilities read has 1 mark per capability. The check passes over that option only when the entry declares
///     all of those capabilities unavailable.
/// </remarks>
/// <param name="name">The capability's key, as an entry's <c>unavailable</c> map writes it.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class CapabilityAttribute(
    string name) : Attribute
{
    /// <summary>
    ///     Gets the capability's key.
    /// </summary>
    public string Name { get; } = name;
}
