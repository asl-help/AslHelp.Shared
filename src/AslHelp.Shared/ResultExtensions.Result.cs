using System;
using System.Diagnostics.CodeAnalysis;
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
