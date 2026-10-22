using System;
using System.Collections.Generic;

namespace AslHelp.Logging;

/// <summary>
///     Writes log lines to every sink that accepts their level.
/// </summary>
public sealed partial class Logger : IDisposable
{
    // Holds what a prefixed view shares with the logger it came from.
    private readonly State _state;

    // Holds whether this logger owns its sinks; a prefixed view shares them and owns none.
    private readonly bool _owns;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Logger"/> class with no sinks.
    /// </summary>
    public Logger()
    {
        _state = new();
        _owns = true;
        Prefix = "";
    }

    private Logger(Logger source, string prefix)
    {
        _state = source._state;
        Prefix = prefix;
    }

    /// <summary>
    ///     Gets the name this logger writes its lines under.
    /// </summary>
    public string Prefix { get; }

    /// <summary>
    ///     Returns a logger over the same sinks and scopes, writing under <paramref name="prefix"/>.
    /// </summary>
    /// <remarks>
    ///     The view owns none of the sinks, so disposing it leaves them open for the logger it came from.
    /// </remarks>
    public Logger WithPrefix(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        return new(this, prefix);
    }

    /// <summary>
    ///     Adds <paramref name="sink"/>, which the logger owns and disposes with itself.
    /// </summary>
    public void Add(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        lock (_state.Sinks)
        {
            _state.Sinks.Add(sink);
            Recompute();
        }
    }

    /// <summary>
    ///     Returns the first sink of type <typeparamref name="T"/>, or <see langword="null"/> when there is none.
    /// </summary>
    public T? GetSink<T>(Func<T, bool>? where = null)
        where T : class, ILogSink
    {
        lock (_state.Sinks)
        {
            foreach (var sink in _state.Sinks)
            {
                if (sink is T match && (where is null || where(match)))
                {
                    return match;
                }
            }
        }

        return null;
    }

    /// <summary>
    ///     Returns whether any sink would write at <paramref name="level"/>, so a caller can skip building one.
    /// </summary>
    /// <remarks>
    ///     Runs once per line before anything is composed, so it reads a threshold the sinks keep up to date instead of
    ///     enumerating the sinks under a lock.
    /// </remarks>
    public bool IsEnabled(LogLevel level)
    {
        return level != LogLevel.Off && level >= _state.Minimum;
    }

    /// <summary>
    ///     Opens an indented scope. Every line written until it is disposed sits one level deeper.
    /// </summary>
    public IDisposable BeginScope()
    {
        int outer = _state.Indent.Value;
        _state.Indent.Value = outer + 1;

        return new Scope(this, outer);
    }

    /// <summary>
    ///     Writes <paramref name="e"/> to every sink that accepts its level.
    /// </summary>
    public void Log(in LogEvent e)
    {
        if (e.Level == LogLevel.Off)
        {
            return;
        }

        int indent = _state.Indent.Value;

        lock (_state.Sinks)
        {
            foreach (var sink in _state.Sinks)
            {
                if (e.Level >= sink.MinimumLevel && sink.MinimumLevel != LogLevel.Off)
                {
                    sink.Emit(e, indent);
                }
            }
        }
    }

    /// <summary>
    ///     Disposes every sink and empties the list, on the logger that owns them; a prefixed view does nothing.
    /// </summary>
    public void Dispose()
    {
        if (!_owns)
        {
            return;
        }

        lock (_state.Sinks)
        {
            foreach (var sink in _state.Sinks)
            {
                sink.Dispose();
            }

            _state.Sinks.Clear();
            Recompute();
        }
    }

    // Runs under the `_state.Sinks` lock.
    private void Recompute()
    {
        var minimum = LogLevel.Off;

        foreach (var sink in _state.Sinks)
        {
            if (sink.MinimumLevel != LogLevel.Off && sink.MinimumLevel < minimum)
            {
                minimum = sink.MinimumLevel;
            }
        }

        _state.Minimum = minimum;
    }
}
