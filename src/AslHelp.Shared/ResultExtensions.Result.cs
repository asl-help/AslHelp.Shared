using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace AslHelp;

/// <summary>
///     Provides methods that read and combine <see cref="Result"/> and <see cref="Result{TValue}"/> values.
/// </summary>
public static partial class ResultExtensions
{
    extension(Result self)
    {
        /// <summary>
        ///     Throws when the result is a failure.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///     The result is a failure.
        /// </exception>
        public void Unwrap()
        {
            if (self.IsErr)
            {
                ThrowUnwrap(self);
            }
        }

        /// <summary>
        ///     Throws with <paramref name="message"/> in front of the result when the result is a failure.
        /// </summary>
        /// <param name="message">The message that describes what was expected.</param>
        /// <exception cref="InvalidOperationException">
        ///     The result is a failure.
        /// </exception>
        public void Expect(string message)
        {
            if (self.IsErr)
            {
                ThrowExpect(self, message);
            }
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
        ///     Replaces the error of a failure with the one <paramref name="fn"/> returns and keeps a success.
        /// </summary>
        /// <param name="fn">The function that turns the error into a new one.</param>
        /// <returns>
        ///     The failure with the new error or the success as it is.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a failure and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public Result MapErr(Func<ResultError, ResultError> fn)
        {
            if (self.IsOk)
            {
                return self;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return Result.Err(fn(self.Error));
        }

        /// <summary>
        ///     Calls <paramref name="onOk"/> on a success or <paramref name="onErr"/> with the error of a failure.
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
        public TOut Match<TOut>(Func<TOut> onOk, Func<ResultError, TOut> onErr)
        {
            if (self.IsOk)
            {
                ArgumentNullException.ThrowIfNull(onOk);

                return onOk();
            }

            ArgumentNullException.ThrowIfNull(onErr);

            return onErr(self.Error);
        }

        /// <summary>
        ///     Returns <paramref name="other"/> after a success and keeps the error of a failure.
        /// </summary>
        /// <param name="other">The result to return after a success.</param>
        /// <returns>
        ///     <paramref name="other"/> or the failure as it is.
        /// </returns>
        [Pure]
        public Result And(Result other)
        {
            return self.IsOk ? other : self;
        }

        /// <summary>
        ///     Returns <paramref name="other"/> after a success and keeps the error of a failure.
        /// </summary>
        /// <typeparam name="TValue">The type of the value of <paramref name="other"/>.</typeparam>
        /// <param name="other">The result to return after a success.</param>
        /// <returns>
        ///     <paramref name="other"/> or a failure with the same error.
        /// </returns>
        [Pure]
        public Result<TValue> And<TValue>(Result<TValue> other)
        {
            return self.IsOk ? other : Result.Err<TValue>(self.Error);
        }

        /// <summary>
        ///     Calls <paramref name="fn"/> after a success and keeps the error of a failure.
        /// </summary>
        /// <param name="fn">The function to call after a success.</param>
        /// <returns>
        ///     The result that <paramref name="fn"/> returns or the failure as it is.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a success and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public Result AndThen(Func<Result> fn)
        {
            if (self.IsErr)
            {
                return self;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return fn();
        }

        /// <summary>
        ///     Calls <paramref name="fn"/> after a success and keeps the error of a failure.
        /// </summary>
        /// <typeparam name="TValue">The type of the value that <paramref name="fn"/> returns.</typeparam>
        /// <param name="fn">The function to call after a success.</param>
        /// <returns>
        ///     The result that <paramref name="fn"/> returns or a failure with the same error.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a success and <paramref name="fn"/> is <see langword="null"/>.
        /// </exception>
        public Result<TValue> AndThen<TValue>(Func<Result<TValue>> fn)
        {
            if (self.IsErr)
            {
                return self.Error;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return fn();
        }

        /// <summary>
        ///     Keeps a success and returns <paramref name="other"/> after a failure.
        /// </summary>
        /// <param name="other">The result to return after a failure.</param>
        /// <returns>
        ///     The success as it is or <paramref name="other"/>.
        /// </returns>
        [Pure]
        public Result Or(Result other)
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
        public Result OrElse(Func<ResultError, Result> fn)
        {
            if (self.IsOk)
            {
                return self;
            }

            ArgumentNullException.ThrowIfNull(fn);

            return fn(self.Error);
        }

        /// <summary>
        ///     Calls <paramref name="action"/> after a success and returns the result as it is.
        /// </summary>
        /// <param name="action">The action to call after a success.</param>
        /// <returns>
        ///     The same result.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     The result is a success and <paramref name="action"/> is <see langword="null"/>.
        /// </exception>
        public Result Inspect(Action action)
        {
            if (self.IsOk)
            {
                ArgumentNullException.ThrowIfNull(action);

                action();
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
        public Result InspectErr(Action<ResultError> action)
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
    private static void ThrowUnwrap(Result result)
    {
        throw new InvalidOperationException($"Cannot unwrap {result}.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowUnwrapErr(Result result)
    {
        throw new InvalidOperationException($"Cannot unwrap the error of {result}.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowExpect(Result result, string message)
    {
        throw new InvalidOperationException($"{message}: {result}");
    }
}
