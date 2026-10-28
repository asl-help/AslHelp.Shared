using System;

namespace AslHelp.Logging;

/// <summary>
///     Represents a destination for log lines.
/// </summary>
public interface ILogSink : IDisposable
{
    /// <summary>
    ///     Gets the lowest level the sink writes.
    /// </summary>
    /// <remarks>
    ///     A <see cref="Logger"/> reads it when the sink is added, so it must not change afterward. Lines below it never
    ///     reach <see cref="Write"/>.
    /// </remarks>
    LogLevel MinimumLevel { get; }

    /// <summary>
    ///     Writes <paramref name="logEvent"/>, indented by <paramref name="indent"/> scopes.
    /// </summary>
    /// <param name="logEvent">The line to write.</param>
    /// <param name="indent">The number of scopes the line is written in.</param>
    /// <remarks>
    ///     Must not throw, because the <see cref="Logger"/> calls every sink in turn and a throw skips the rest.
    /// </remarks>
    void Write(in LogEvent logEvent, int indent);
}
