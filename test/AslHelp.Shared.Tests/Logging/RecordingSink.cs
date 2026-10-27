using AslHelp.Logging;
using System.Collections.ObjectModel;

namespace AslHelp.Tests.Logging;

/// <summary>
///     Represents a sink that keeps every event it receives, so a test can assert on the level gate.
/// </summary>
internal sealed class RecordingSink(
    LogLevel minimumLevel) : ILogSink
{
    /// <summary>
    ///     Gets every event this sink was handed, in order, with the scope depth it arrived at.
    /// </summary>
    public Collection<(LogEvent Event, int Indent)> Events { get; } = [];

    /// <summary>
    ///     Gets a value indicating whether the logger disposed this sink.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <inheritdoc/>
    public LogLevel MinimumLevel { get; } = minimumLevel;

    /// <inheritdoc/>
    public void Write(in LogEvent logEvent, int indent)
    {
        Events.Add((logEvent, indent));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        IsDisposed = true;
    }
}
