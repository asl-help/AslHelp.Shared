using NUnit.Framework;
using System;

namespace AslHelp.Tests;

public partial class ResultExtensionsTests
{
    // Unwrap returns the value of a success.
    [Test]
    public void UnwrapOfT_OnOk_ReturnsTheValue()
    {
        Assert.That(Result.Ok(42).Unwrap(), Is.EqualTo(42));
    }

    // Unwrap on a failure throws with the error message, so the failure is visible where it surfaced.
    [Test]
    public void UnwrapOfT_OnErr_ThrowsWithTheErrorMessage()
    {
        Assert.That(
            () => Result.Err<int>(new TestError("boom")).Unwrap(),
            Throws.InvalidOperationException.With.Message.EqualTo("Cannot unwrap the value of Result<System.Int32>.Err('boom')."));
    }

    // Expect returns the value of a success.
    [Test]
    public void ExpectOfT_OnOk_ReturnsTheValue()
    {
        Assert.That(Result.Ok(42).Expect("unused"), Is.EqualTo(42));
    }

    // Expect on a failure throws with the given message in front of the result.
    [Test]
    public void ExpectOfT_OnErr_ThrowsWithBothMessages()
    {
        Assert.That(
            () => Result.Err<int>(new TestError("boom")).Expect("Reading the level failed"),
            Throws.InvalidOperationException.With.Message.EqualTo("Reading the level failed: Result<System.Int32>.Err('boom')"));
    }

    // UnwrapOr returns the value of a success and the fallback on a failure.
    [Test]
    public void UnwrapOr_ForEitherState_FallsBackOnlyOnErr()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok(42).UnwrapOr(7), Is.EqualTo(42));
            Assert.That(Result.Err<int>(new TestError("boom")).UnwrapOr(7), Is.EqualTo(7));
        }
    }

    // UnwrapOrDefault returns the value of a success and the type's default on a failure.
    [Test]
    public void UnwrapOrDefault_ForEitherState_FallsBackOnlyOnErr()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok(42).UnwrapOrDefault(), Is.EqualTo(42));
            Assert.That(Result.Err<int>(new TestError("boom")).UnwrapOrDefault(), Is.Zero);
        }
    }

    // UnwrapOrElse returns the value of a success without calling the fallback.
    [Test]
    public void UnwrapOrElse_OnOk_ReturnsTheValue()
    {
        Assert.That(Result.Ok(42).UnwrapOrElse(null!), Is.EqualTo(42));
    }

    // UnwrapOrElse computes a value from the error of a failure.
    [Test]
    public void UnwrapOrElse_OnErr_ComputesTheFallback()
    {
        Assert.That(Result.Err<int>(new TestError("boom")).UnwrapOrElse(static e => e.Message.Length), Is.EqualTo(4));
    }

    // UnwrapOrElse refuses a null fallback on a failure, where it would call it.
    [Test]
    public void UnwrapOrElse_WithNullFnOnErr_Throws()
    {
        Assert.That(
            () => Result.Err<int>(new TestError("boom")).UnwrapOrElse(null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("fn"));
    }

    // TryUnwrap hands out the value of a success.
    [Test]
    public void TryUnwrapOfT_OnOk_HandsOutTheValue()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok(42).TryUnwrap(out var value), Is.True);
            Assert.That(value, Is.EqualTo(42));
        }
    }

    // TryUnwrap hands out the error of a failure.
    [Test]
    public void TryUnwrapOfT_OnErr_HandsOutTheError()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Err<int>(new TestError("boom")).TryUnwrap(out _), Is.False);
            Assert.That(Result.Err<int>(new TestError("boom")).TryUnwrap(out _, out var error), Is.False);
            Assert.That(error!.Message, Is.EqualTo("boom"));
        }
    }

    // UnwrapErr returns the error of a failure.
    [Test]
    public void UnwrapErrOfT_OnErr_ReturnsTheError()
    {
        Assert.That(Result.Err<int>(new TestError("boom")).UnwrapErr().Message, Is.EqualTo("boom"));
    }

    // UnwrapErr on a success throws with the result in the message.
    [Test]
    public void UnwrapErrOfT_OnOk_ThrowsWithTheResult()
    {
        Assert.That(
            () => Result.Ok(42).UnwrapErr(),
            Throws.InvalidOperationException.With.Message.EqualTo("Cannot unwrap the error of Result<System.Int32>.Ok(42)."));
    }

    // ExpectErr on a success throws with the given message in front of the result.
    [Test]
    public void ExpectErrOfT_OnOk_ThrowsWithBothMessages()
    {
        Assert.That(
            () => Result.Ok(42).ExpectErr("Expected a failure"),
            Throws.InvalidOperationException.With.Message.EqualTo("Expected a failure: Result<System.Int32>.Ok(42)"));
    }

    // TryUnwrapErr hands out the error only on a failure.
    [Test]
    public void TryUnwrapErrOfT_ForEitherState_HandsOutTheErrorOnErr()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok(42).TryUnwrapErr(out _), Is.False);
            Assert.That(Result.Err<int>(new TestError("boom")).TryUnwrapErr(out var error), Is.True);
            Assert.That(error!.Message, Is.EqualTo("boom"));
        }
    }
}
