using System.Runtime.CompilerServices;
using System.Text;

namespace AslHelp.Logging;

/// <summary>
///     Builds an interpolated trace message only when a sink writes at its level.
/// </summary>
/// <remarks>
///     Trace lines sit inside loops that run thousands of times per tick, where composing a message no sink writes is
///     the whole cost. The compiler passes the pieces in one at a time, so a level that is off allocates nothing and a
///     call site still reads <c>log.LogTrace($"...")</c>. Every level has its own handler because the handler's
///     constructor checks the level before the method it's passed to runs.
/// </remarks>
[InterpolatedStringHandler]
public readonly ref struct LogTraceInterpolatedStringHandler
{
    private readonly StringBuilder? _builder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LogTraceInterpolatedStringHandler"/> struct.
    /// </summary>
    /// <param name="literalLength">The number of characters in the message's literal parts.</param>
    /// <param name="formattedCount">The number of interpolated values in the message.</param>
    /// <param name="logger">The logger the message is destined for.</param>
    /// <param name="isEnabled">Whether the message will be built, which tells the compiler to skip every append.</param>
    public LogTraceInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
    {
        _builder = LogMessage.CreateBuilder(logger, LogLevel.Trace, literalLength, formattedCount, out isEnabled);
    }

    /// <summary>
    ///     Appends one of the message's literal parts.
    /// </summary>
    /// <param name="value">The literal.</param>
    public readonly void AppendLiteral(string value)
    {
        LogMessage.AppendLiteral(_builder, value);
    }

    /// <summary>
    ///     Appends one of the message's interpolated values.
    /// </summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="value">The value.</param>
    public readonly void AppendFormatted<T>(T value)
    {
        LogMessage.AppendFormatted(_builder, value);
    }

    /// <summary>
    ///     Appends one of the message's interpolated values, rendered with a format.
    /// </summary>
    /// <typeparam name="T">The value's type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="format">The format to render it with, e.g. <c>"X"</c>.</param>
    public readonly void AppendFormatted<T>(T value, string? format)
    {
        LogMessage.AppendFormatted(_builder, value, format);
    }

    /// <summary>
    ///     Appends an address, rendered with a format.
    /// </summary>
    /// <param name="value">The address.</param>
    /// <param name="format">The format to render it with, e.g. <c>"X"</c>.</param>
    public readonly void AppendFormatted(nint value, string? format)
    {
        LogMessage.AppendAddress(_builder, (ulong)value, format);
    }

    /// <inheritdoc cref="AppendFormatted(nint, string?)"/>
    public readonly void AppendFormatted(nuint value, string? format)
    {
        LogMessage.AppendAddress(_builder, value, format);
    }

    internal readonly bool IsEnabled => _builder is not null;

    internal readonly string Message => _builder?.ToString() ?? "";
}

/// <inheritdoc cref="LogTraceInterpolatedStringHandler"/>
[InterpolatedStringHandler]
public readonly ref struct LogDebugInterpolatedStringHandler
{
    private readonly StringBuilder? _builder;

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler(int, int, Logger, out bool)"/>
    public LogDebugInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
    {
        _builder = LogMessage.CreateBuilder(logger, LogLevel.Debug, literalLength, formattedCount, out isEnabled);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendLiteral(string)"/>
    public readonly void AppendLiteral(string value)
    {
        LogMessage.AppendLiteral(_builder, value);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted{T}(T)"/>
    public readonly void AppendFormatted<T>(T value)
    {
        LogMessage.AppendFormatted(_builder, value);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted{T}(T, string?)"/>
    public readonly void AppendFormatted<T>(T value, string? format)
    {
        LogMessage.AppendFormatted(_builder, value, format);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted(nint, string?)"/>
    public readonly void AppendFormatted(nint value, string? format)
    {
        LogMessage.AppendAddress(_builder, (ulong)value, format);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted(nint, string?)"/>
    public readonly void AppendFormatted(nuint value, string? format)
    {
        LogMessage.AppendAddress(_builder, value, format);
    }

    internal readonly bool IsEnabled => _builder is not null;

    internal readonly string Message => _builder?.ToString() ?? "";
}

/// <inheritdoc cref="LogTraceInterpolatedStringHandler"/>
[InterpolatedStringHandler]
public readonly ref struct LogInformationInterpolatedStringHandler
{
    private readonly StringBuilder? _builder;

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler(int, int, Logger, out bool)"/>
    public LogInformationInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
    {
        _builder = LogMessage.CreateBuilder(logger, LogLevel.Information, literalLength, formattedCount, out isEnabled);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendLiteral(string)"/>
    public readonly void AppendLiteral(string value)
    {
        LogMessage.AppendLiteral(_builder, value);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted{T}(T)"/>
    public readonly void AppendFormatted<T>(T value)
    {
        LogMessage.AppendFormatted(_builder, value);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted{T}(T, string?)"/>
    public readonly void AppendFormatted<T>(T value, string? format)
    {
        LogMessage.AppendFormatted(_builder, value, format);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted(nint, string?)"/>
    public readonly void AppendFormatted(nint value, string? format)
    {
        LogMessage.AppendAddress(_builder, (ulong)value, format);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted(nint, string?)"/>
    public readonly void AppendFormatted(nuint value, string? format)
    {
        LogMessage.AppendAddress(_builder, value, format);
    }

    internal readonly bool IsEnabled => _builder is not null;

    internal readonly string Message => _builder?.ToString() ?? "";
}

/// <inheritdoc cref="LogTraceInterpolatedStringHandler"/>
[InterpolatedStringHandler]
public readonly ref struct LogWarningInterpolatedStringHandler
{
    private readonly StringBuilder? _builder;

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler(int, int, Logger, out bool)"/>
    public LogWarningInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
    {
        _builder = LogMessage.CreateBuilder(logger, LogLevel.Warning, literalLength, formattedCount, out isEnabled);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendLiteral(string)"/>
    public readonly void AppendLiteral(string value)
    {
        LogMessage.AppendLiteral(_builder, value);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted{T}(T)"/>
    public readonly void AppendFormatted<T>(T value)
    {
        LogMessage.AppendFormatted(_builder, value);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted{T}(T, string?)"/>
    public readonly void AppendFormatted<T>(T value, string? format)
    {
        LogMessage.AppendFormatted(_builder, value, format);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted(nint, string?)"/>
    public readonly void AppendFormatted(nint value, string? format)
    {
        LogMessage.AppendAddress(_builder, (ulong)value, format);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted(nint, string?)"/>
    public readonly void AppendFormatted(nuint value, string? format)
    {
        LogMessage.AppendAddress(_builder, value, format);
    }

    internal readonly bool IsEnabled => _builder is not null;

    internal readonly string Message => _builder?.ToString() ?? "";
}

/// <inheritdoc cref="LogTraceInterpolatedStringHandler"/>
[InterpolatedStringHandler]
public readonly ref struct LogErrorInterpolatedStringHandler
{
    private readonly StringBuilder? _builder;

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler(int, int, Logger, out bool)"/>
    public LogErrorInterpolatedStringHandler(int literalLength, int formattedCount, Logger logger, out bool isEnabled)
    {
        _builder = LogMessage.CreateBuilder(logger, LogLevel.Error, literalLength, formattedCount, out isEnabled);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendLiteral(string)"/>
    public readonly void AppendLiteral(string value)
    {
        LogMessage.AppendLiteral(_builder, value);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted{T}(T)"/>
    public readonly void AppendFormatted<T>(T value)
    {
        LogMessage.AppendFormatted(_builder, value);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted{T}(T, string?)"/>
    public readonly void AppendFormatted<T>(T value, string? format)
    {
        LogMessage.AppendFormatted(_builder, value, format);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted(nint, string?)"/>
    public readonly void AppendFormatted(nint value, string? format)
    {
        LogMessage.AppendAddress(_builder, (ulong)value, format);
    }

    /// <inheritdoc cref="LogTraceInterpolatedStringHandler.AppendFormatted(nint, string?)"/>
    public readonly void AppendFormatted(nuint value, string? format)
    {
        LogMessage.AppendAddress(_builder, value, format);
    }

    internal readonly bool IsEnabled => _builder is not null;

    internal readonly string Message => _builder?.ToString() ?? "";
}
