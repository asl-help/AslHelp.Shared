using AslHelp.Logging;
using System.Collections.ObjectModel;

namespace AslHelp.Tests.Logging;

public partial class LoggerTests
{
    /// <summary>
    ///     Represents a sink that keeps every event it receives, so a test can assert on the level gate.
    /// </summary>
    private sealed class RecordingSink(
        LogLevel minimumLevel) : ILogSink
    {
        /// <summary>
        ///     Gets every event this sink was handed, in order, with the scope depth it arrived at.
        /// </summary>
        public Collection<(LogEvent Event, int Indent)> Lines { get; } = [];

        /// <summary>
        ///     Gets a value indicating whether the logger disposed this sink.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <inheritdoc/>
        public LogLevel MinimumLevel { get; } = minimumLevel;

        /// <inheritdoc/>
        public void Emit(in LogEvent e, int indent)
        {
            Lines.Add((e, indent));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }
    }
}
