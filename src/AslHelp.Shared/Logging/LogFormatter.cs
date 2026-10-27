using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace AslHelp.Logging;

/// <summary>
///     Renders a <see cref="LogEvent"/> into the line a sink writes.
/// </summary>
/// <param name="logEvent">The event to render.</param>
/// <param name="indent">The number of enclosing scopes to indent by.</param>
/// <returns>
///     The line.
/// </returns>
public delegate string LogFormatter(in LogEvent logEvent, int indent);

/// <summary>
///     Provides extension members for <see cref="LogFormatter"/>, including the formatters a sink uses when it's given
///     none.
/// </summary>
public static class LogFormatterExtensions
{
    extension(LogFormatter)
    {
        /// <summary>
        ///     Picks the formatter for a sink that writes at <paramref name="minimumLevel"/> and above.
        /// </summary>
        /// <param name="minimumLevel">The lowest level the sink writes.</param>
        /// <param name="includeTimestamp">
        ///     <see langword="true"/> to start each line with a timestamp and <see langword="false"/> for a sink that stamps
        ///     its own lines.
        /// </param>
        /// <returns>
        ///     <see cref="FormatWithTimestampAndLocation"/> or <see cref="FormatWithLocation"/> at
        ///     <see cref="LogLevel.Trace"/>, because the location is only worth the width while someone reads every step.
        ///     <see cref="FormatWithTimestamp"/> or <see cref="FormatPlain"/> otherwise.
        /// </returns>
        public static LogFormatter Create(LogLevel minimumLevel, bool includeTimestamp = true)
        {
            return (minimumLevel, includeTimestamp) switch
            {
                (LogLevel.Trace, true) => FormatWithTimestampAndLocation,
                (LogLevel.Trace, false) => FormatWithLocation,
                (_, true) => FormatWithTimestamp,
                _ => FormatPlain,
            };
        }

        /// <summary>
        ///     Renders <c>[level] [prefix] message</c>, for a sink that stamps its own lines.
        /// </summary>
        public static string FormatPlain(in LogEvent logEvent, int indent)
        {
            StringBuilder sb = new();

            AppendHeader(sb, logEvent, indent);

            return sb.ToString();
        }

        /// <summary>
        ///     Renders <c>[timestamp] [level] [prefix] message</c>.
        /// </summary>
        public static string FormatWithTimestamp(in LogEvent logEvent, int indent)
        {
            StringBuilder sb = new();

            AppendTimestamp(sb);
            AppendHeader(sb, logEvent, indent);

            return sb.ToString();
        }

        /// <summary>
        ///     Renders <see cref="FormatPlain"/> followed by <c>(member @ File.cs:line)</c>, where it was written.
        /// </summary>
        /// <remarks>
        ///     What a host that stamps its own lines shows at trace.
        /// </remarks>
        public static string FormatWithLocation(in LogEvent logEvent, int indent)
        {
            StringBuilder sb = new();

            AppendHeader(sb, logEvent, indent);
            AppendLocation(sb, logEvent);

            return sb.ToString();
        }

        /// <summary>
        ///     Renders <see cref="FormatWithTimestamp"/> followed by <c>(member @ File.cs:line)</c>, where it was written.
        /// </summary>
        /// <remarks>
        ///     Only worth the width where someone is reading at trace, so the other formatters leave the location out.
        /// </remarks>
        public static string FormatWithTimestampAndLocation(in LogEvent logEvent, int indent)
        {
            StringBuilder sb = new();

            AppendTimestamp(sb);
            AppendHeader(sb, logEvent, indent);
            AppendLocation(sb, logEvent);

            return sb.ToString();
        }
    }

    private static void AppendTimestamp(StringBuilder sb)
    {
        _ = sb.Append('[')
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Append("] ");
    }

    private static void AppendHeader(StringBuilder sb, in LogEvent logEvent, int indent)
    {
        const int IndentWidth = 2;
        const string Nested = "=> ";

        _ = sb.Append('[').Append(logEvent.Level.ToShortName()).Append("] ");

        if (logEvent.Prefix.Length > 0)
        {
            _ = sb.Append('[').Append(logEvent.Prefix).Append("] ");
        }

        // Marks a nested line with an arrow, since indentation alone leaves a reader counting whitespace.
        _ = sb.Append(' ', Math.Max(indent, 0) * IndentWidth);

        if (indent > 0)
        {
            _ = sb.Append(Nested);
        }

        _ = sb.Append(logEvent.Message);
    }

    // Renders `(member @ File.cs:line)` for the library's lines and a script's alike. A host that can't map a compiled
    // action back to a source line records no line, so the file alone says where.
    private static void AppendLocation(StringBuilder sb, in LogEvent logEvent)
    {
        if (logEvent.MemberName.Length == 0)
        {
            return;
        }

        _ = sb.Append(" (").Append(logEvent.MemberName);

        if (logEvent.FilePath.Length > 0)
        {
            _ = sb.Append(" @ ").Append(Path.GetFileName(logEvent.FilePath));

            if (logEvent.LineNumber > 0)
            {
                _ = sb.Append(':').Append(logEvent.LineNumber);
            }
        }

        _ = sb.Append(')');
    }
}
