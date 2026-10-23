using AslHelp.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AslHelp.Options.Capabilities;

/// <summary>
///     Represents a capability an entry declares it cannot serve, along with the reason.
/// </summary>
[Options]
[method: SetsRequiredMembers]
public sealed class UnavailableCapability()
{
    /// <summary>
    ///     Gets or sets why the entry cannot serve the capability, as a <see cref="CapabilityState"/> spelling.
    /// </summary>
    [Choice(typeof(CapabilityState))]
    public required string? State { get; set; }

    /// <summary>
    ///     Gets or sets the reason in words, which the refusal repeats.
    /// </summary>
    public required string? Reason { get; set; }

    /// <summary>
    ///     Checks whether an entry declares it cannot serve a capability.
    /// </summary>
    /// <param name="unavailable">
    ///     The capabilities the entry declares it cannot serve, by key. <see langword="null"/> when there are none.
    /// </param>
    /// <param name="capability">The capability's key.</param>
    /// <param name="surface">The surface the capability serves, as the refusal words it.</param>
    /// <returns>
    ///     A success when the entry serves the capability, otherwise a failed result that repeats the entry's reason.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="capability"/> or <paramref name="surface"/> is <see langword="null"/>.
    /// </exception>
    public static Result Check(IReadOnlyDictionary<string, UnavailableCapability>? unavailable, string capability, string surface)
    {
        ArgumentNullException.ThrowIfNull(capability);
        ArgumentNullException.ThrowIfNull(surface);

        return unavailable is { } declared && declared.TryGetValue(capability, out var reason)
            ? CapabilityError.Unavailable(surface, reason.Reason)
            : Result.Ok();
    }

    /// <summary>
    ///     Throws when an entry declares it cannot serve a capability, repeating the entry's reason.
    /// </summary>
    /// <param name="unavailable">
    ///     The capabilities the entry declares it cannot serve, by key. <see langword="null"/> when there are none.
    /// </param>
    /// <param name="capability">The capability's key.</param>
    /// <param name="surface">The surface the capability serves, as the refusal words it.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="capability"/> or <paramref name="surface"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The entry declares <paramref name="capability"/> unavailable.
    /// </exception>
    public static void Require(IReadOnlyDictionary<string, UnavailableCapability>? unavailable, string capability, string surface)
    {
        if (Check(unavailable, capability, surface).TryUnwrapErr(out var refused))
        {
            throw new InvalidOperationException(refused.Message);
        }
    }
}
