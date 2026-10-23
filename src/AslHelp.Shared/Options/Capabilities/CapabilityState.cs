using AslHelp.Options;
namespace AslHelp.Options.Capabilities;

/// <summary>
///     Specifies why an entry cannot serve a capability.
/// </summary>
public enum CapabilityState
{
    /// <summary>
    ///     The builds the entry covers lack what the capability reads.
    /// </summary>
    NotApplicable,

    /// <summary>
    ///     The builds have what the capability reads, but no one has measured where.
    /// </summary>
    Unresolved,
}
