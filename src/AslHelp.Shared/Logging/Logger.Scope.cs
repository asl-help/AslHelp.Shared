using System;

namespace AslHelp.Logging;

public partial class Logger
{
    // Restores the depth the scope opened at rather than stepping it down, since a scope disposed in another
    // asynchronous context would otherwise step that context's depth below zero.
    private sealed class Scope(
        Logger logger,
        int outer) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            logger._state.Indent.Value = outer;
        }
    }
}
