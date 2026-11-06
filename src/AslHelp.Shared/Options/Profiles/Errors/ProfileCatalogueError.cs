namespace AslHelp.Options.Profiles.Errors;

/// <summary>
///     Represents a failure to resolve an entry from a <see cref="ProfileCatalogue{TKey, TOptions}"/>.
/// </summary>
internal sealed record ProfileCatalogueError : ResultError
{
    private ProfileCatalogueError(string message, bool hasRejectedOverrides = false)
    {
        Message = message;
        HasRejectedOverrides = hasRejectedOverrides;
    }

    /// <inheritdoc/>
    public override string Message { get; }

    /// <summary>
    ///     Gets a value indicating whether the entry refused the overrides copied over it instead of no entry covering
    ///     the target.
    /// </summary>
    public bool HasRejectedOverrides { get; }

    /// <summary>
    ///     Creates the error for a target no entry covers at or below its version in its partition and pointer size.
    /// </summary>
    /// <param name="engine">The subject of the entries, such as <c>Widget</c>.</param>
    /// <param name="target">The target's partition and version.</param>
    /// <param name="pointerSize">The target's pointer size.</param>
    /// <returns>
    ///     The error.
    /// </returns>
    public static ProfileCatalogueError ProfileNotFound(string engine, string target, int pointerSize)
    {
        return new(SR.FormatProfileNotFound(engine, target, pointerSize * 8));
    }

    /// <summary>
    ///     Creates the error for overrides that leave the entry without a required value or set a value the entry
    ///     doesn't accept.
    /// </summary>
    /// <param name="reason">The message of the failed validation.</param>
    /// <returns>
    ///     The error.
    /// </returns>
    public static ProfileCatalogueError OverridesRejected(string reason)
    {
        return new(reason, hasRejectedOverrides: true);
    }

    /// <summary>
    ///     Creates the error for an entry registered as a parsed profile without its document, which leaves no
    ///     document to copy overrides or scoped values onto.
    /// </summary>
    /// <param name="description">The entry's key as an error writes it.</param>
    /// <returns>
    ///     The error.
    /// </returns>
    public static ProfileCatalogueError DocumentNotRegistered(string description)
    {
        return new(SR.FormatDocumentNotRegistered(description));
    }
}
