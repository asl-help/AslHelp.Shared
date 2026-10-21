using System;

namespace AslHelp;

/// <summary>
///     Represents a <see cref="ResultError"/> that wraps a caught exception.
/// </summary>
public sealed record ExceptionError : ResultError
{
    private readonly string? _message;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExceptionError"/> class from <paramref name="exception"/>, with
    ///     the exception's own message.
    /// </summary>
    /// <param name="exception">The exception to wrap.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="exception"/> is <see langword="null"/>.
    /// </exception>
    public ExceptionError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Exception = exception;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExceptionError"/> class from <paramref name="exception"/>, with
    ///     <paramref name="message"/> in place of the exception's own.
    /// </summary>
    /// <param name="exception">The exception to wrap.</param>
    /// <param name="message">The message describing the failure.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="exception"/> or <paramref name="message"/> is <see langword="null"/>.
    /// </exception>
    public ExceptionError(Exception exception, string message)
        : this(exception)
    {
        ArgumentNullException.ThrowIfNull(message);

        _message = message;
    }

    /// <summary>
    ///     Gets the wrapped exception.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    ///     Gets the message given to the constructor when there is one and the wrapped exception's own message
    ///     otherwise.
    /// </summary>
    public override string Message => _message ?? Exception.Message;
}
