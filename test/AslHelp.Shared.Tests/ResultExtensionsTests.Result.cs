using NUnit.Framework;
using System;

namespace AslHelp.Tests;

[TestFixture]
public partial class ResultExtensionsTests
{
    // Unwrap on a success returns without throwing.
    [Test]
    public void Unwrap_OnOk_DoesNotThrow()
    {
        Assert.That(() => Result.Ok().Unwrap(), Throws.Nothing);
    }

    // Unwrap on a failure throws with the error message, so the failure is visible where it surfaced.
    [Test]
    public void Unwrap_OnErr_ThrowsWithTheErrorMessage()
    {
        Assert.That(
            () => Result.Err(new TestError("boom")).Unwrap(),
            Throws.InvalidOperationException.With.Message.EqualTo("Cannot unwrap Result.Err('boom')."));
    }

    // Expect on a success returns without throwing.
    [Test]
    public void Expect_OnOk_DoesNotThrow()
    {
        Assert.That(() => Result.Ok().Expect("unused"), Throws.Nothing);
    }

    // Expect on a failure throws with the given message in front of the result.
    [Test]
    public void Expect_OnErr_ThrowsWithBothMessages()
    {
        Assert.That(
            () => Result.Err(new TestError("boom")).Expect("Writing the level failed"),
            Throws.InvalidOperationException.With.Message.EqualTo("Writing the level failed: Result.Err('boom')"));
    }

    // UnwrapErr returns the error of a failure.
    [Test]
    public void UnwrapErr_OnErr_ReturnsTheError()
    {
        Assert.That(Result.Err(new TestError("boom")).UnwrapErr().Message, Is.EqualTo("boom"));
    }

    // UnwrapErr on a success throws with the result in the message.
    [Test]
    public void UnwrapErr_OnOk_ThrowsWithTheResult()
    {
        Assert.That(
            () => Result.Ok().UnwrapErr(),
            Throws.InvalidOperationException.With.Message.EqualTo("Cannot unwrap the error of Result.Ok()."));
    }

    // ExpectErr on a success throws with the given message in front of the result.
    [Test]
    public void ExpectErr_OnOk_ThrowsWithBothMessages()
    {
        Assert.That(
            () => Result.Ok().ExpectErr("Expected a failure"),
            Throws.InvalidOperationException.With.Message.EqualTo("Expected a failure: Result.Ok()"));
    }

    // TryUnwrapErr hands out the error of a failure.
    [Test]
    public void TryUnwrapErr_OnErr_HandsOutTheError()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Err(new TestError("boom")).TryUnwrapErr(out var error), Is.True);
            Assert.That(error!.Message, Is.EqualTo("boom"));
        }
    }

    // TryUnwrapErr on a success hands out nothing.
    [Test]
    public void TryUnwrapErr_OnOk_ReturnsFalse()
    {
        Assert.That(Result.Ok().TryUnwrapErr(out _), Is.False);
    }
}
