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

    // MapErr replaces the error of a failure.
    [Test]
    public void MapErr_OnErr_ReplacesTheError()
    {
        var result = Result.Err(new TestError("boom")).MapErr(static e => new TestError($"wrapped {e.Message}"));

        Assert.That(result.Error!.Message, Is.EqualTo("wrapped boom"));
    }

    // MapErr keeps a success without calling the function.
    [Test]
    public void MapErr_OnOk_KeepsTheSuccess()
    {
        Assert.That(Result.Ok().MapErr(null!).IsOk, Is.True);
    }

    // MapErr refuses a null function on a failure, where it would call it.
    [Test]
    public void MapErr_WithNullFnOnErr_Throws()
    {
        Assert.That(
            () => Result.Err(new TestError("boom")).MapErr(null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("fn"));
    }

    // Match calls the success function on a success and the error function on a failure.
    [Test]
    public void Match_ForEitherState_CallsTheMatchingFunction()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok().Match(static () => "ok", null!), Is.EqualTo("ok"));
            Assert.That(Result.Err(new TestError("boom")).Match(null!, static e => $"err {e.Message}"), Is.EqualTo("err boom"));
        }
    }

    // Match refuses a null function for the branch it calls.
    [Test]
    public void Match_WithNullFnForTheState_Throws()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => Result.Ok().Match(null!, static _ => "err"),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("onOk"));
            Assert.That(
                () => Result.Err(new TestError("boom")).Match(static () => "ok", null!),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("onErr"));
        }
    }
}
