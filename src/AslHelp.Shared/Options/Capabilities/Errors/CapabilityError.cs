using AslHelp.Options;
namespace AslHelp.Options.Capabilities.Errors;

/// <summary>
///     Represents a capability that an options entry declares it can't serve, with the entry's reason.
/// </summary>
internal sealed record CapabilityError : ResultError
{
    private CapabilityError(string message)
    {
        Message = message;
    }

    /// <inheritdoc/>
    public override string Message { get; }

    /// <summary>
    ///     Creates the error for <paramref name="surface"/>, which the entry declares unavailable.
    /// </summary>
    /// <param name="surface">The surface the caller asked for, such as <c>scene manager</c>.</param>
    /// <param name="reason">The reason the entry gives.</param>
    /// <returns>
    ///     The error.
    /// </returns>
    public static CapabilityError Unavailable(string surface, string? reason)
    {
        return new($"The options declare the {surface} unavailable: {reason}");
    }
}
