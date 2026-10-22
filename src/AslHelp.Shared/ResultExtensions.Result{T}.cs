using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
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

        /// <summary>
        ///     Transforms the value of a success with <paramref name="fn"/> and keeps the error of a failure.
        /// </summary>
        /// <typeparam name="TOther">The type of the transformed value.</typeparam>
        /// <param name="fn">The function that transforms the value.</param>
        /// <returns>
        ///     A success with the transformed value or the failure with the same error.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a success and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public Result<TOther> Map<TOther>(Func<TValue, TOther> fn)
        {
            if (self.IsErr)
            {
                return self.Error;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return Result.Ok(fn(self.Value!));
        }

        /// <summary>
        ///     Replaces the error of a failure with the one <paramref name="fn"/> returns and keeps a success.
        /// </summary>
        /// <param name="fn">The function that turns the error into a new one.</param>
        /// <returns>
        ///     The failure with the new error or the success as it is.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a failure and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public Result<TValue> MapErr(Func<ResultError, ResultError> fn)
        {
            if (self.IsOk)
            {
                return self;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return Result.Err<TValue>(fn(self.Error));
        }

        /// <summary>
        ///     Transforms the value of a success with <paramref name="fn"/> and returns <paramref name="default"/> on a
        ///     failure.
        /// </summary>
        /// <typeparam name="TOther">The type of the transformed value.</typeparam>
        /// <param name="default">The value to return on a failure.</param>
        /// <param name="fn">The function that transforms the value.</param>
        /// <returns>
        ///     The transformed value or <paramref name="default"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a success and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public TOther MapOr<TOther>(TOther @default, Func<TValue, TOther> fn)
        {
            if (self.IsErr)
            {
                return @default;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return fn(self.Value!);
        }

        /// <summary>
        ///     Transforms the value of a success with <paramref name="fn"/> and the error of a failure with
        ///     <paramref name="fallback"/>.
        /// </summary>
        /// <typeparam name="TOther">The type of the transformed value.</typeparam>
        /// <param name="fn">The function that transforms the value.</param>
        /// <param name="fallback">The function that turns the error into a value.</param>
        /// <returns>
        ///     The value that the called function returns.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The function for the result's state is <see langword="null"/>.
        /// </exception>
        public TOther MapOrElse<TOther>(Func<TValue, TOther> fn, Func<ResultError, TOther> fallback)
        {
            if (self.IsOk)
            {
                ArgumentNullException.ThrowIfNull(fn);

                return fn(self.Value!);
            }

            ArgumentNullException.ThrowIfNull(fallback);

            return fallback(self.Error);
        }

        /// <summary>
        ///     Calls <paramref name="onOk"/> with the value of a success or <paramref name="onErr"/> with the error of
        ///     a failure.
        /// </summary>
        /// <typeparam name="TOut">The type that both functions return.</typeparam>
        /// <param name="onOk">The function to call on a success.</param>
        /// <param name="onErr">The function to call on a failure.</param>
        /// <returns>
        ///     The value that the called function returns.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The function for the result's state is <see langword="null"/>.
        /// </exception>
        public TOut Match<TOut>(Func<TValue, TOut> onOk, Func<ResultError, TOut> onErr)
        {
            if (self.IsOk)
            {
                ArgumentNullException.ThrowIfNull(onOk);

                return onOk(self.Value!);
            }

            ArgumentNullException.ThrowIfNull(onErr);

            return onErr(self.Error);
        }

        /// <summary>
        ///     Returns <paramref name="other"/> after a success and keeps the error of a failure.
        /// </summary>
        /// <param name="other">The result to return after a success.</param>
        /// <returns>
        ///     <paramref name="other"/> or a failure with the same error.
        /// </returns>
        [Pure]
        public Result And(Result other)
        {
            return self.IsOk ? other : Result.Err(self.Error);
        }

        /// <summary>
        ///     Returns <paramref name="other"/> after a success and keeps the error of a failure.
        /// </summary>
        /// <typeparam name="TOther">The type of the value of <paramref name="other"/>.</typeparam>
        /// <param name="other">The result to return after a success.</param>
        /// <returns>
        ///     <paramref name="other"/> or a failure with the same error.
        /// </returns>
        [Pure]
        public Result<TOther> And<TOther>(Result<TOther> other)
        {
            return self.IsOk ? other : Result.Err<TOther>(self.Error);
        }

        /// <summary>
        ///     Calls <paramref name="fn"/> with the value of a success and keeps the error of a failure.
        /// </summary>
        /// <param name="fn">The function to call with the value.</param>
        /// <returns>
        ///     The result that <paramref name="fn"/> returns or a failure with the same error.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a success and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public Result AndThen(Func<TValue, Result> fn)
        {
            if (self.IsErr)
            {
                return self.Error;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return fn(self.Value!);
        }

        /// <summary>
        ///     Calls <paramref name="fn"/> with the value of a success and keeps the error of a failure.
        /// </summary>
        /// <typeparam name="TOther">The type of the value that <paramref name="fn"/> returns.</typeparam>
        /// <param name="fn">The function to call with the value.</param>
        /// <returns>
        ///     The result that <paramref name="fn"/> returns or a failure with the same error.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a success and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public Result<TOther> AndThen<TOther>(Func<TValue, Result<TOther>> fn)
        {
            if (self.IsErr)
            {
                return self.Error;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return fn(self.Value!);
        }

        /// <summary>
        ///     Keeps a success and returns <paramref name="other"/> after a failure.
        /// </summary>
        /// <param name="other">The result to return after a failure.</param>
        /// <returns>
        ///     The success as it is or <paramref name="other"/>.
        /// </returns>
        [Pure]
        public Result<TValue> Or(Result<TValue> other)
        {
            return self.IsOk ? self : other;
        }

        /// <summary>
        ///     Keeps a success and calls <paramref name="fn"/> with the error of a failure.
        /// </summary>
        /// <param name="fn">The function to call with the error.</param>
        /// <returns>
        ///     The success as it is or the result that <paramref name="fn"/> returns.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a failure and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public Result<TValue> OrElse(Func<ResultError, Result<TValue>> fn)
        {
            if (self.IsOk)
            {
                return self;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return fn(self.Error);
        }

        /// <summary>
        ///     Calls <paramref name="action"/> with the value of a success and returns the result as it is.
        /// </summary>
        /// <param name="action">The action to call with the value.</param>
        /// <returns>
        ///     The same result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a success and <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        public Result<TValue> Inspect(Action<TValue> action)
        {
            if (self.IsOk)
            {
                ArgumentNullException.ThrowIfNull(action);

                action(self.Value!);
            }

            return self;
        }

        /// <summary>
        ///     Calls <paramref name="action"/> with the error of a failure and returns the result as it is.
        /// </summary>
        /// <param name="action">The action to call with the error.</param>
        /// <returns>
        ///     The same result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a failure and <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        public Result<TValue> InspectErr(Action<ResultError> action)
        {
            if (self.IsErr)
            {
                ArgumentNullException.ThrowIfNull(action);

                action(self.Error);
            }

            return self;
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
