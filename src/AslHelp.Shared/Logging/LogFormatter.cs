using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace AslHelp.Logging;

/// <summary>
///     Renders a <see cref="LogEvent"/> into the line a sink writes.
/// </summary>
/// <param name="e">The event to render.</param>
/// <param name="indent">The number of enclosing scopes to indent by.</param>
/// <returns>
///     The line.
/// </returns>
public delegate string LogFormatter(in LogEvent e, int indent);

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
        /// <param name="stamped">
        ///     <see langword="true"/> to start each line with a timestamp and <see langword="false"/> for a sink that stamps
        ///     its own lines.
        /// </param>
        /// <returns>
        ///     <see cref="Traced"/> or <see cref="Located"/> at <see cref="LogLevel.Trace"/>, because the location is only
        ///     worth the width while someone reads every step. <see cref="Stamped"/> or <see cref="Plain"/> otherwise.
        /// </returns>
        public static LogFormatter ForLevel(LogLevel minimumLevel, bool stamped = true)
        {
            return (minimumLevel, stamped) switch
            {
                (LogLevel.Trace, true) => Traced,
                (LogLevel.Trace, false) => Located,
                (_, true) => Stamped,
                _ => Plain,
            };
        }

        /// <summary>
        ///     Renders <c>[level] [prefix] message</c>, for a sink that stamps its own lines.
        /// </summary>
        public static string Plain(in LogEvent e, int indent)
        {
            StringBuilder sb = new();

            Head(sb, e, indent);

            return sb.ToString();
        }

        /// <summary>
        ///     Renders <c>[timestamp] [level] [prefix] message</c>.
        /// </summary>
        public static string Stamped(in LogEvent e, int indent)
        {
            StringBuilder sb = new();

            Stamp(sb);
            Head(sb, e, indent);

            return sb.ToString();
        }

        /// <summary>
        ///     Renders <see cref="Plain"/> followed by <c>(member @ File.cs:line)</c>, where it was written.
        /// </summary>
        /// <remarks>
        ///     What a host that stamps its own lines shows at trace.
        /// </remarks>
        public static string Located(in LogEvent e, int indent)
        {
            StringBuilder sb = new();

            Head(sb, e, indent);
            Location(sb, e);

            return sb.ToString();
        }

        /// <summary>
        ///     Renders <see cref="Stamped"/> followed by <c>(member @ File.cs:line)</c>, where it was written.
        /// </summary>
        /// <remarks>
        ///     Only worth the width where someone is reading at trace, so the other formatters leave the location out.
        /// </remarks>
        public static string Traced(in LogEvent e, int indent)
        {
            StringBuilder sb = new();

            Stamp(sb);
            Head(sb, e, indent);
            Location(sb, e);

            return sb.ToString();
        }
    }

    private static void Stamp(StringBuilder sb)
    {
        _ = sb.Append('[')
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture))
            .Append("] ");
    }

    private static void Head(StringBuilder sb, in LogEvent e, int indent)
    {
        const int IndentWidth = 2;
        const string Nested = "=> ";

        _ = sb.Append('[').Append(e.Level.Tag()).Append("] ");

        if (e.Prefix.Length > 0)
        {
            _ = sb.Append('[').Append(e.Prefix).Append("] ");
        }

        // Marks a nested line with an arrow, since indentation alone leaves a reader counting whitespace.
        _ = sb.Append(' ', Math.Max(indent, 0) * IndentWidth);

        if (indent > 0)
        {
            _ = sb.Append(Nested);
        }

        _ = sb.Append(e.Message);
    }

    // Renders `(member @ File.cs:line)` for the library's lines and a script's alike. A host that can't map a compiled
    // action back to a source line records no line, so the file alone says where.
    private static void Location(StringBuilder sb, in LogEvent e)
    {
        if (e.MemberName.Length == 0)
        {
            return;
        }

        _ = sb.Append(" (").Append(e.MemberName);

        if (e.FilePath.Length > 0)
        {
            _ = sb.Append(" @ ").Append(Path.GetFileName(e.FilePath));

            if (e.LineNumber > 0)
            {
                _ = sb.Append(':').Append(e.LineNumber);
            }
        }

        _ = sb.Append(')');
    }
}
