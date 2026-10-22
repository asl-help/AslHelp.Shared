using NUnit.Framework;
using System;

namespace AslHelp.Tests;

[TestFixture]
public class ResultTests
{
    // Ok() is a success with no error.
    [Test]
    public void Ok_Always_IsOk()
    {
        var result = Result.Ok();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.IsErr, Is.False);
            Assert.That(result.Error, Is.Null);
        }
    }

    // Err(error) is a failure that reports the error it was given.
    [Test]
    public void Err_WithError_IsErr()
    {
        var result = Result.Err(new TestError("boom"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.IsOk, Is.False);
            Assert.That(result.Error!.Message, Is.EqualTo("boom"));
        }
    }

    // Err refuses a null error instead of building a failure that reports nothing.
    [Test]
    public void Err_WithNullError_Throws()
    {
        Assert.That(() => Result.Err(null!), Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("error"));
    }

    // A default Result is a success, since only an error makes a failure.
    [Test]
    public void Default_Always_IsOk()
    {
        Result result = default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.IsErr, Is.False);
        }
    }

    // An error converts to a failed result on its own.
    [Test]
    public void ImplicitConversion_FromError_IsErr()
    {
        Result result = new TestError("boom");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.Error!.Message, Is.EqualTo("boom"));
        }
    }

    // The implicit conversion refuses a null error the same way Err does.
    [Test]
    public void ImplicitConversion_FromNullError_Throws()
    {
        ResultError error = null!;

        Assert.That(() => (Result)error, Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("error"));
    }

    // ToString prints the state and, on a failure, the message.
    [Test]
    public void ToString_ForEitherState_PrintsTheStateAndTheMessage()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok().ToString(), Is.EqualTo("Result.Ok()"));
            Assert.That(Result.Err(new TestError("boom")).ToString(), Is.EqualTo("Result.Err('boom')"));
        }
    }

    // Ok(value) is a success with the value.
    [Test]
    public void OkOfT_WithValue_IsOkWithTheValue()
    {
        var result = Result.Ok(42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(result.Error, Is.Null);
        }
    }

    // Err<T>(error) is a failure whose value is the type's default.
    [Test]
    public void ErrOfT_WithError_IsErrWithDefaultValue()
    {
        var result = Result.Err<int>(new TestError("boom"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.Value, Is.Zero);
            Assert.That(result.Error!.Message, Is.EqualTo("boom"));
        }
    }

    // Err<T> refuses a null error instead of building a failure that reports nothing.
    [Test]
    public void ErrOfT_WithNullError_Throws()
    {
        Assert.That(
            () => Result.Err<int>(null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("error"));
    }
}
