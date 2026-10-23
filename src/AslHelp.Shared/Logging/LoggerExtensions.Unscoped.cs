using System;

namespace AslHelp.Logging;

public static partial class LoggerExtensions
{
    private sealed class Unscoped : IDisposable
    {
        public static readonly Unscoped Instance = new();

        public void Dispose() { }
    }
}
