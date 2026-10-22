using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace AslHelp.Tests;

[TestFixture]
public class ResultOfTTests
{
    // A success may have a null value where the type allows one.
    [Test]
    public void Ok_WithNullValue_IsOkWithNull()
    {
        var result = Result.Ok<string?>(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Value, Is.Null);
        }
    }

    // A default Result<T> is a success whose value is the type's default.
    [Test]
    public void Default_Always_IsOkWithDefaultValue()
    {
        Result<int> result = default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Value, Is.Zero);
        }
    }

    // A value converts to a success on its own.
    [Test]
    public void ImplicitConversion_FromValue_IsOk()
    {
        Result<int> result = 42;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
        }
    }

    // An error converts to a failure on its own.
    [Test]
    public void ImplicitConversion_FromError_IsErr()
    {
        Result<int> result = new TestError("boom");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsErr, Is.True);
            Assert.That(result.Error!.Message, Is.EqualTo("boom"));
        }
    }

    // The implicit conversion refuses a null error the same way Err<T> does.
    [Test]
    public void ImplicitConversion_FromNullError_Throws()
    {
        ResultError error = null!;

        Assert.That(() => (Result<int>)error, Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("error"));
    }

    // An exception is a value like any other, so Result<Exception> is a success with the exception as its value.
    [Test]
    public void ImplicitConversion_FromException_IsOkWithTheExceptionAsValue()
    {
        InvalidOperationException exception = new("boom");
        Result<Exception> result = exception;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsOk, Is.True);
            Assert.That(result.Value, Is.SameAs(exception));
        }
    }

    // ToString prints the value type and the state, with the value or the message.
    [Test]
    public void ToString_ForEitherState_PrintsTheTypeAndTheStateAndTheContent()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Result.Ok(42).ToString(), Is.EqualTo("Result<System.Int32>.Ok(42)"));
            Assert.That(Result.Err<int>(new TestError("boom")).ToString(), Is.EqualTo("Result<System.Int32>.Err('boom')"));
        }
    }

    // ToString prints <null> for a null value, so it doesn't look like a Result without a value.
    [Test]
    public void ToString_WithNullValue_PrintsNull()
    {
        Assert.That(Result.Ok<string?>(null).ToString(), Is.EqualTo("Result<System.String>.Ok(<null>)"));
    }

    // ToString spells the value type the way Type.ToString does, generic arguments and declaring types included.
    [Test]
    public void ToString_ForComposedTypes_PrintsTheClrSpelling()
    {
        TestError error = new("boom");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                Result.Err<List<int>>(error).ToString(),
                Is.EqualTo("Result<System.Collections.Generic.List`1[System.Int32]>.Err('boom')"));
            Assert.That(
                Result.Err<Dictionary<int, string>.Enumerator>(error).ToString(),
                Is.EqualTo("Result<System.Collections.Generic.Dictionary`2+Enumerator[System.Int32,System.String]>.Err('boom')"));
            Assert.That(Result.Err<int?>(error).ToString(), Is.EqualTo("Result<System.Nullable`1[System.Int32]>.Err('boom')"));
        }
    }

    // Results are equal when their values or their errors are equal, through IEquatable so nothing boxes.
    [Test]
    public void Equals_ForEqualContent_IsTrue()
    {
        var ok = Result.Ok(42);
        var err = Result.Err<int>(new TestError("boom"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ok, Is.InstanceOf<IEquatable<Result<int>>>());
            Assert.That(ok, Is.EqualTo(Result.Ok(42)));
            Assert.That(err, Is.EqualTo(Result.Err<int>(new TestError("boom"))));
            Assert.That(ok, Is.Not.EqualTo(Result.Ok(43)));
            Assert.That(ok, Is.Not.EqualTo(err));
        }
    }
}
