using System.Collections.Generic;
using System.Threading;

namespace AslHelp.Logging;

public partial class Logger
{
    private sealed class State
    {
        private volatile LogLevel _minimum = LogLevel.Off;

        public List<ILogSink> Sinks { get; } = [];

        public AsyncLocal<int> Indent { get; } = new();

        // Is read without the lock on every line, so it's volatile; it's written only under the lock on `Sinks`.
        public LogLevel Minimum
        {
            get => _minimum;
            set => _minimum = value;
        }
    }
}
