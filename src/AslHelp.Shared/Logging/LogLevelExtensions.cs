namespace AslHelp.Logging;

/// <summary>
///     Provides extension methods for <see cref="LogLevel"/>.
/// </summary>
internal static class LogLevelExtensions
{
    /// <summary>
    ///     Gets the 4-character tag a formatted line shows for <paramref name="level"/>.
    /// </summary>
    /// <param name="level">The level.</param>
    /// <returns>
    ///     The tag, such as <c>info</c>. <c>off </c> for <see cref="LogLevel.Off"/> and for a value that isn't a
    ///     defined level.
    /// </returns>
    public static string ToShortName(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            _ => "off ",
        };
    }
}
