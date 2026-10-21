using System;
using System.Diagnostics.CodeAnalysis;

namespace AslHelp;

/// <summary>
///     Represents the outcome of an operation that yields a <typeparamref name="TValue"/> on success.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <remarks>
///     A result is a failure when it has an <see cref="Error"/>, so <c>default(Result&lt;TValue&gt;)</c> is a success
///     whose value is <c>default(TValue)</c>.
/// </remarks>
public readonly record struct Result<TValue>
{
    internal Result(TValue value)
    {
        Value = value;
        Error = null;
    }

    internal Result(ResultError error)
    {
        Value = default;
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
    ///     Gets the value on a success and the default value of <typeparamref name="TValue"/> on a failure.
    /// </summary>
    /// <remarks>
    ///     <see cref="IsOk"/> doesn't mark <see cref="Value"/> as not <see langword="null"/> because a success can have
    ///     a <see langword="null"/> value where <typeparamref name="TValue"/> allows one.
    /// </remarks>
    public TValue? Value { get; }

    /// <summary>
    ///     Gets the failure on a failure and <see langword="null"/> on a success.
    /// </summary>
    public ResultError? Error { get; }

    /// <summary>
    ///     Converts <paramref name="value"/> to a successful result.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>
    ///     The result.
    /// </returns>
    /// <remarks>
    ///     C# doesn't apply this conversion when <typeparamref name="TValue"/> is an interface, so a method returning
    ///     <c>Result&lt;IReadOnlyList&lt;int&gt;&gt;</c> returns <c>Result.Ok(value)</c> instead.
    /// </remarks>
    public static implicit operator Result<TValue>(TValue value)
    {
        return new(value);
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
    public static implicit operator Result<TValue>(ResultError error)
    {
        return Result.Err<TValue>(error);
    }

    /// <summary>
    ///     Formats the result as <c>Result&lt;Type&gt;.Ok(value)</c> or <c>Result&lt;Type&gt;.Err('message')</c>, with
    ///     <c>&lt;null&gt;</c> for a <see langword="null"/> value.
    /// </summary>
    /// <returns>
    ///     The formatted result.
    /// </returns>
    public override string ToString()
    {
        var type = typeof(TValue);

        return IsOk
            ? $"Result<{type}>.Ok({(Value is null ? "<null>" : Value)})"
            : $"Result<{type}>.Err('{Error.Message}')";
    }
}
