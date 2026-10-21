namespace AslHelp;

/// <summary>
///     Represents the failure a failed <see cref="Result"/> reports.
/// </summary>
public abstract record ResultError
{
    /// <summary>
    ///     Gets the message describing the failure.
    /// </summary>
    public abstract string Message { get; }

    /// <summary>
    ///     Formats the error as its full type name and its message, as in <c>AslHelp.ExceptionError: message</c>.
    /// </summary>
    public sealed override string ToString()
    {
        return $"{GetType().FullName}: {Message}";
    }
}
