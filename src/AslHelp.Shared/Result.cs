using System;
using System.Diagnostics.CodeAnalysis;

namespace AslHelp;

/// <summary>
///     Represents the outcome of an operation that yields no value.
/// </summary>
/// <remarks>
///     A result is a failure when it has an <see cref="Error"/>, so <c>default(Result)</c> is a success.
/// </remarks>
public readonly record struct Result
{
    private Result(ResultError? error)
    {
        Error = error;
    }

    /// <summary>
    ///     Gets a value indicating whether the result is a success.
    /// </summary>
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsOk => Error is null;

    /// <summary>
    ///     Gets a value indicating whether the result is a failure.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsErr => Error is not null;

    /// <summary>
    ///     Gets the failure on a failure and <see langword="null"/> on a success.
    /// </summary>
    public ResultError? Error { get; }

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    /// <returns>
    ///     The result.
    /// </returns>
    public static Result Ok()
    {
        return new(null);
    }

    /// <summary>
    ///     Creates a failed result with <paramref name="error"/>.
    /// </summary>
    /// <param name="error">The failure.</param>
    /// <returns>
    ///     The result.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="error"/> is <see langword="null"/>.
    /// </exception>
    public static Result Err(ResultError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new(error);
    }

    /// <summary>
    ///     Creates a successful result with <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>
    ///     The result.
    /// </returns>
    public static Result<TValue> Ok<TValue>(TValue value)
    {
        return new(value);
    }

    /// <summary>
    ///     Creates a failed <see cref="Result{TValue}"/> with <paramref name="error"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value a success would have.</typeparam>
    /// <param name="error">The failure.</param>
    /// <returns>
    ///     The result.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="error"/> is <see langword="null"/>.
    /// </exception>
    public static Result<TValue> Err<TValue>(ResultError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new(error);
    }

    /// <summary>
    ///     Converts <paramref name="error"/> to a failed result.
    /// </summary>
    /// <param name="error">The failure.</param>
    /// <returns>
    ///     The result.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="error"/> is <see langword="null"/>.
    /// </exception>
    public static implicit operator Result(ResultError error)
    {
        return Err(error);
    }

    /// <summary>
    ///     Formats the result as <c>Result.Ok()</c> or <c>Result.Err('message')</c>.
    /// </summary>
    /// <returns>
    ///     The formatted result.
    /// </returns>
    public override string ToString()
    {
        return IsOk ? "Result.Ok()" : $"Result.Err('{Error.Message}')";
    }
}
