namespace AslHelp.Logging;

/// <summary>
///     Represents one log line and the place in the source it was written from.
/// </summary>
/// <param name="Level">The level the line was written at.</param>
/// <param name="Prefix">The name the line is written under, such as the library's or a script's.</param>
/// <param name="Message">The text of the line.</param>
/// <param name="MemberName">The member the line was written from.</param>
/// <param name="FilePath">The source file the line was written from.</param>
/// <param name="LineNumber">The source line the line was written from, or 0 when it isn't known.</param>
public readonly record struct LogEvent(
    LogLevel Level,
    string Prefix,
    string Message,
    string MemberName,
    string FilePath,
    int LineNumber);
