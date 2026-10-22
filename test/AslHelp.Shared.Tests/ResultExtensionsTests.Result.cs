using NUnit.Framework;
using System;
using System.Collections.Generic;

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

    // And returns the other result after a success.
    [Test]
    public void And_OnOk_ReturnsTheOtherResult()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok().And(Result.Ok(42)).Value, Is.EqualTo(42));
            Assert.That(Result.Ok().And(Result.Err(new TestError("second"))).Error!.Message, Is.EqualTo("second"));
        }
    }

    // And keeps the first failure.
    [Test]
    public void And_OnErr_KeepsTheFirstError()
    {
        var first = Result.Err(new TestError("boom"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.And(Result.Ok(42)).Error!.Message, Is.EqualTo("boom"));
            Assert.That(first.And(Result.Ok()).Error!.Message, Is.EqualTo("boom"));
        }
    }

    // AndThen calls the continuation after a success.
    [Test]
    public void AndThen_OnOk_ReturnsTheContinuationsResult()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok().AndThen(static () => Result.Ok(42)).Value, Is.EqualTo(42));
            Assert.That(Result.Ok().AndThen(static () => Result.Err(new TestError("second"))).Error!.Message, Is.EqualTo("second"));
        }
    }

    // AndThen keeps a failure without calling the continuation.
    [Test]
    public void AndThen_OnErr_KeepsTheError()
    {
        var first = Result.Err(new TestError("boom"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.AndThen((Func<Result<int>>)null!).Error!.Message, Is.EqualTo("boom"));
            Assert.That(first.AndThen(null!).Error!.Message, Is.EqualTo("boom"));
        }
    }

    // AndThen refuses a null continuation after a success, where it would call it.
    [Test]
    public void AndThen_WithNullFnOnOk_Throws()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => Result.Ok().AndThen(null!),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("fn"));
            Assert.That(
                () => Result.Ok().AndThen((Func<Result<int>>)null!),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("fn"));
        }
    }

    // Or keeps a success and returns the other result after a failure.
    [Test]
    public void Or_ForEitherState_ReplacesOnlyAFailure()
    {
        var err = Result.Err(new TestError("boom"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok().Or(err).IsOk, Is.True);
            Assert.That(err.Or(Result.Ok()).IsOk, Is.True);
        }
    }

    // OrElse calls the recovery with the error of a failure.
    [Test]
    public void OrElse_OnErr_ReturnsTheRecoverysResult()
    {
        var result = Result.Err(new TestError("boom")).OrElse(static e => Result.Err(new TestError($"wrapped {e.Message}")));

        Assert.That(result.Error!.Message, Is.EqualTo("wrapped boom"));
    }

    // OrElse keeps a success without calling the recovery.
    [Test]
    public void OrElse_OnOk_KeepsTheSuccess()
    {
        Assert.That(Result.Ok().OrElse(null!).IsOk, Is.True);
    }

    // OrElse refuses a null recovery on a failure, where it would call it.
    [Test]
    public void OrElse_WithNullFnOnErr_Throws()
    {
        Assert.That(
            () => Result.Err(new TestError("boom")).OrElse(null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("fn"));
    }

    // Inspect runs the action after a success and returns the same result.
    [Test]
    public void Inspect_OnOk_RunsTheActionAndReturnsTheResult()
    {
        var seen = new List<string>();
        var ok = Result.Ok();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ok.Inspect(() => seen.Add("ok")), Is.EqualTo(ok));
            Assert.That(seen, Is.EqualTo(["ok"]));
        }
    }

    // Inspect skips the action on a failure.
    [Test]
    public void Inspect_OnErr_SkipsTheAction()
    {
        var err = Result.Err(new TestError("boom"));

        Assert.That(err.Inspect(null!), Is.EqualTo(err));
    }

    // InspectErr runs the action with the error of a failure and returns the same result.
    [Test]
    public void InspectErr_OnErr_RunsTheActionAndReturnsTheResult()
    {
        var seen = new List<string>();
        var err = Result.Err(new TestError("boom"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(err.InspectErr(e => seen.Add(e.Message)), Is.EqualTo(err));
            Assert.That(seen, Is.EqualTo(["boom"]));
        }
    }

    // InspectErr skips the action on a success.
    [Test]
    public void InspectErr_OnOk_SkipsTheAction()
    {
        var ok = Result.Ok();

        Assert.That(ok.InspectErr(null!), Is.EqualTo(ok));
    }

    // Inspect and InspectErr refuse a null action for the state they run on.
    [Test]
    public void Inspect_WithNullActionForTheState_Throws()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => Result.Ok().Inspect(null!),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("action"));
            Assert.That(
                () => Result.Err(new TestError("boom")).InspectErr(null!),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("action"));
        }
    }
}
