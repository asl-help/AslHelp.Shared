namespace AslHelp.Logging;

/// <summary>
///     Specifies how severe a log line is.
/// </summary>
public enum LogLevel
{
    /// <summary>
    ///     Every step, down to single reads.
    /// </summary>
    Trace,

    /// <summary>
    ///     Detail that helps while a script is being written.
    /// </summary>
    Debug,

    /// <summary>
    ///     What a script author asks to see, and the steps around it.
    /// </summary>
    Information,

    /// <summary>
    ///     Something the run can continue past, but the author should hear about.
    /// </summary>
    Warning,

    /// <summary>
    ///     Something that stopped working.
    /// </summary>
    Error,

    /// <summary>
    ///     No lines at all.
    /// </summary>
    Off,
}
