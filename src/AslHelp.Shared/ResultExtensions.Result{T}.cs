using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AslHelp;

public static partial class ResultExtensions
{
    extension<TValue>(Result<TValue> self)
    {
        /// <summary>
        ///     Returns the value of a success and throws on a failure.
        /// </summary>
        /// <returns>
        ///     The value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The result is a failure.
        /// </exception>
        public TValue Unwrap()
        {
            if (self.IsErr)
            {
                ThrowUnwrap(self);
            }

            return self.Value!;
        }

        /// <summary>
        ///     Returns the value of a success and throws with <paramref name="message"/> in front of the result on a
        ///     failure.
        /// </summary>
        /// <param name="message">The message that describes what was expected.</param>
        /// <returns>
        ///     The value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The result is a failure.
        /// </exception>
        public TValue Expect(string message)
        {
            if (self.IsErr)
            {
                ThrowExpect(self, message);
            }

            return self.Value!;
        }

        /// <summary>
        ///     Returns the value of a success and <paramref name="default"/> on a failure.
        /// </summary>
        /// <param name="default">The value to return on a failure.</param>
        /// <returns>
        ///     The value or <paramref name="default"/>.
        /// </returns>
        public TValue UnwrapOr(TValue @default)
        {
            return self.IsOk ? self.Value! : @default;
        }

        /// <summary>
        ///     Returns the value of a success and the default value of <typeparamref name="TValue"/> on a failure.
        /// </summary>
        /// <returns>
        ///     The value or the default value of <typeparamref name="TValue"/>.
        /// </returns>
        public TValue? UnwrapOrDefault()
        {
            return self.Value;
        }

        /// <summary>
        ///     Returns the value of a success and computes a value from the error with <paramref name="fn"/> on a
        ///     failure.
        /// </summary>
        /// <param name="fn">The function that turns the error into a value.</param>
        /// <returns>
        ///     The value or the computed value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a failure and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public TValue UnwrapOrElse(Func<ResultError, TValue> fn)
        {
            if (self.IsOk)
            {
                return self.Value!;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return fn(self.Error);
        }

        /// <summary>
        ///     Gets the value of a success.
        /// </summary>
        /// <param name="value">The value on a success and <see langword="default"/> on a failure.</param>
        /// <returns>
        ///     <see langword="true"/> on a success and <see langword="false"/> on a failure.
        /// </returns>
        public bool TryUnwrap([MaybeNullWhen(false)] out TValue value)
        {
            value = self.Value;
            return self.IsOk;
        }

        /// <summary>
        ///     Gets the value of a success or the error of a failure.
        /// </summary>
        /// <param name="value">The value on a success and <see langword="default"/> on a failure.</param>
        /// <param name="error">The error on a failure and <see langword="null"/> on a success.</param>
        /// <returns>
        ///     <see langword="true"/> on a success and <see langword="false"/> on a failure.
        /// </returns>
        public bool TryUnwrap([MaybeNullWhen(false)] out TValue value, [NotNullWhen(false)] out ResultError? error)
        {
            value = self.Value;
            error = self.Error;
            return self.IsOk;
        }

        /// <summary>
        ///     Returns the error of a failure and throws on a success.
        /// </summary>
        /// <returns>
        ///     The error.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The result is a success.
        /// </exception>
        public ResultError UnwrapErr()
        {
            if (self.IsOk)
            {
                ThrowUnwrapErr(self);
            }

            return self.Error;
        }

        /// <summary>
        ///     Returns the error of a failure and throws with <paramref name="message"/> in front of the result on a
        ///     success.
        /// </summary>
        /// <param name="message">The message that describes what was expected.</param>
        /// <returns>
        ///     The error.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     The result is a success.
        /// </exception>
        public ResultError ExpectErr(string message)
        {
            if (self.IsOk)
            {
                ThrowExpect(self, message);
            }

            return self.Error;
        }

        /// <summary>
        ///     Gets the error of a failure.
        /// </summary>
        /// <param name="error">The error on a failure and <see langword="null"/> on a success.</param>
        /// <returns>
        ///     <see langword="true"/> on a failure and <see langword="false"/> on a success.
        /// </returns>
        public bool TryUnwrapErr([NotNullWhen(true)] out ResultError? error)
        {
            error = self.Error;
            return self.IsErr;
        }
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnwrap<TValue>(Result<TValue> result)
    {
        throw new InvalidOperationException($"Cannot unwrap the value of {result}.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnwrapErr<TValue>(Result<TValue> result)
    {
        throw new InvalidOperationException($"Cannot unwrap the error of {result}.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowExpect<TValue>(Result<TValue> result, string message)
    {
        throw new InvalidOperationException($"{message}: {result}");
    }
}
