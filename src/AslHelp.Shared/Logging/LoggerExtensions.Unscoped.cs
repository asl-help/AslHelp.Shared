using System;

namespace AslHelp.Logging;

public static partial class LoggerExtensions
{
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose() { }
    }
}
