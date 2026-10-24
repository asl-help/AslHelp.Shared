namespace AslHelp.Options.Profiles.Errors;

/// <summary>
///     Represents a failure to resolve an entry from a <see cref="ProfileCatalogue{TKey, TOptions}"/>.
/// </summary>
internal sealed record ProfileCatalogueError : ResultError
{
    private ProfileCatalogueError(string message, bool refusedOverrides = false)
    {
        Message = message;
        RefusedOverrides = refusedOverrides;
    }

    /// <inheritdoc/>
    public override string Message { get; }

    /// <summary>
    ///     Gets a value indicating whether the entry refused the overrides copied over it instead of no entry covering
    ///     the target.
    /// </summary>
    public bool RefusedOverrides { get; }

    /// <summary>
    ///     Creates the error for a target no entry covers at or below its version in its partition and pointer size.
    /// </summary>
    /// <param name="engine">The subject of the entries, such as <c>Widget</c>.</param>
    /// <param name="target">The target's partition and version.</param>
    /// <param name="pointerSize">The target's pointer size.</param>
    /// <returns>
    ///     The error.
    /// </returns>
    public static ProfileCatalogueError NoProfile(string engine, string target, int pointerSize)
    {
        return new($"No {engine} profile is registered at or below {engine} {target}, {pointerSize * 8} bit.");
    }

    /// <summary>
    ///     Creates the error for overrides that leave the entry without a required value or set a value the entry
    ///     doesn't accept.
    /// </summary>
    /// <param name="reason">The message of the failed validation.</param>
    /// <returns>
    ///     The error.
    /// </returns>
    public static ProfileCatalogueError OverridesDoNotFit(string reason)
    {
        return new(reason, refusedOverrides: true);
    }

    /// <summary>
    ///     Creates the error for an entry registered as a parsed profile without its document, which leaves no
    ///     document to copy overrides or scoped values onto.
    /// </summary>
    /// <param name="description">The entry's key as an error writes it.</param>
    /// <returns>
    ///     The error.
    /// </returns>
    public static ProfileCatalogueError NoDocument(string description)
    {
        return new(
            $"The entry for {description} was registered as a parsed profile rather than from its document, "
            + "so there is nothing to plant values onto. Register the document to override or scope it.");
    }
}
