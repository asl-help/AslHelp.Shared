using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AslHelp.Logging;

/// <summary>
///     Provides extension methods for writing to a <see cref="Logger"/>.
/// </summary>
/// <remarks>
///     Every method records the member, file and line the call is written in. The methods that take an interpolated
///     message compose it only when a sink writes at the method's level.
/// </remarks>
public static partial class LoggerExtensions
{
    private const string Handled = "The handler's constructor takes the same logger and rejects a null one, and the compiler runs it first.";

    /// <summary>
    ///     Writes <paramref name="message"/> at <see cref="LogLevel.Trace"/>.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="message">The message.</param>
    /// <param name="memberName">The member the call is written in.</param>
    /// <param name="filePath">The source file the call is written in.</param>
    /// <param name="lineNumber">The source line the call is written on.</param>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = Handled)]
    public static void LogTrace(
        this Logger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))] ref LogTraceInterpolatedStringHandler message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        // Returns early because a disabled handler skips composing the message but can't skip this call.
        if (!message.IsEnabled)
        {
            return;
        }

        logger.Log(new LogEvent(LogLevel.Trace, logger.Prefix, message.Message, memberName, filePath, lineNumber));
    }

    /// <summary>
    ///     Writes <paramref name="message"/> at <see cref="LogLevel.Debug"/>.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="message">The message.</param>
    /// <param name="memberName">The member the call is written in.</param>
    /// <param name="filePath">The source file the call is written in.</param>
    /// <param name="lineNumber">The source line the call is written on.</param>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = Handled)]
    public static void LogDebug(
        this Logger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))] ref LogDebugInterpolatedStringHandler message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!message.IsEnabled)
        {
            return;
        }

        logger.Log(new LogEvent(LogLevel.Debug, logger.Prefix, message.Message, memberName, filePath, lineNumber));
    }

    /// <summary>
    ///     Writes <paramref name="message"/> at <see cref="LogLevel.Information"/>.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="message">The message.</param>
    /// <param name="memberName">The member the call is written in.</param>
    /// <param name="filePath">The source file the call is written in.</param>
    /// <param name="lineNumber">The source line the call is written on.</param>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = Handled)]
    public static void LogInformation(
        this Logger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))] ref LogInformationInterpolatedStringHandler message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!message.IsEnabled)
        {
            return;
        }

        logger.Log(new LogEvent(LogLevel.Information, logger.Prefix, message.Message, memberName, filePath, lineNumber));
    }

    /// <summary>
    ///     Writes <paramref name="message"/> at <see cref="LogLevel.Warning"/>.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="message">The message.</param>
    /// <param name="memberName">The member the call is written in.</param>
    /// <param name="filePath">The source file the call is written in.</param>
    /// <param name="lineNumber">The source line the call is written on.</param>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = Handled)]
    public static void LogWarning(
        this Logger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))] ref LogWarningInterpolatedStringHandler message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!message.IsEnabled)
        {
            return;
        }

        logger.Log(new LogEvent(LogLevel.Warning, logger.Prefix, message.Message, memberName, filePath, lineNumber));
    }

    /// <summary>
    ///     Writes <paramref name="message"/> at <see cref="LogLevel.Error"/>.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="message">The message.</param>
    /// <param name="memberName">The member the call is written in.</param>
    /// <param name="filePath">The source file the call is written in.</param>
    /// <param name="lineNumber">The source line the call is written on.</param>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = Handled)]
    public static void LogError(
        this Logger logger,
        [InterpolatedStringHandlerArgument(nameof(logger))] ref LogErrorInterpolatedStringHandler message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (!message.IsEnabled)
        {
            return;
        }

        logger.Log(new LogEvent(LogLevel.Error, logger.Prefix, message.Message, memberName, filePath, lineNumber));
    }

    /// <summary>
    ///     Writes <paramref name="message"/> at <paramref name="level"/>.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="level">The level to write at.</param>
    /// <param name="message">The message.</param>
    /// <param name="memberName">The member the call is written in.</param>
    /// <param name="filePath">The source file the call is written in.</param>
    /// <param name="lineNumber">The source line the call is written on.</param>
    /// <remarks>
    ///     Takes a message that is already a string. An interpolated message goes to the method for its level instead,
    ///     because that method skips composing a message no sink writes.
    /// </remarks>
    public static void Log(
        this Logger logger,
        LogLevel level,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (!logger.IsEnabled(level))
        {
            return;
        }

        logger.Log(new LogEvent(level, logger.Prefix, message, memberName, filePath, lineNumber));
    }

    /// <summary>
    ///     Writes <paramref name="message"/> at <paramref name="level"/> as a header, then indents every line until the
    ///     returned scope is disposed.
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="level">The level to write the header at.</param>
    /// <param name="message">The header.</param>
    /// <param name="memberName">The member the call is written in.</param>
    /// <param name="filePath">The source file the call is written in.</param>
    /// <param name="lineNumber">The source line the call is written on.</param>
    /// <returns>
    ///     The scope. It indents nothing when no sink writes at <paramref name="level"/>.
    /// </returns>
    /// <remarks>
    ///     Don't open a scope around a <see langword="yield"/>, because the consumer's lines would be indented between
    ///     items.
    /// </remarks>
    public static IDisposable BeginScope(
        this Logger logger,
        LogLevel level,
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (!logger.IsEnabled(level))
        {
            return Unscoped.Instance;
        }

        logger.Log(new LogEvent(level, logger.Prefix, message, memberName, filePath, lineNumber));

        return logger.BeginScope();
    }
}
