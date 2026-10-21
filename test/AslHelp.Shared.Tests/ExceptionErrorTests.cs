using NUnit.Framework;
using System;

namespace AslHelp.Tests;

[TestFixture]
public class ExceptionErrorTests
{
    // The error takes the exception's own message.
    [Test]
    public void Message_FromException_IsTheExceptionsMessage()
    {
        ExceptionError error = new(new InvalidOperationException("kaboom"));

        Assert.That(error.Message, Is.EqualTo("kaboom"));
    }

    // A given message replaces the exception's own.
    [Test]
    public void Message_WithOwnMessage_IsTheGivenMessage()
    {
        ExceptionError error = new(new InvalidOperationException("kaboom"), "friendlier");

        Assert.That(error.Message, Is.EqualTo("friendlier"));
    }

    // The wrapped exception stays reachable, so a throw later can keep its stack trace.
    [Test]
    public void Exception_Always_IsTheWrappedException()
    {
        InvalidOperationException exception = new("kaboom");
        ExceptionError error = new(exception);

        Assert.That(error.Exception, Is.SameAs(exception));
    }

    // The error refuses a null exception.
    [Test]
    public void Constructor_FromNullException_Throws()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => new ExceptionError(null!),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("exception"));
            Assert.That(
                () => new ExceptionError(null!, "friendlier"),
                Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("exception"));
        }
    }

    // The error refuses a null message instead of falling back to the exception's own message.
    [Test]
    public void Constructor_WithNullMessage_Throws()
    {
        Assert.That(
            () => new ExceptionError(new InvalidOperationException("kaboom"), null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("message"));
    }

    // ToString prints the error's own full type name with the message in use.
    [Test]
    public void ToString_ForEitherMessage_PrintsErrorTypeAndMessage()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                new ExceptionError(new InvalidOperationException("kaboom")).ToString(),
                Is.EqualTo("AslHelp.ExceptionError: kaboom"));
            Assert.That(
                new ExceptionError(new InvalidOperationException("orig"), "custom").ToString(),
                Is.EqualTo("AslHelp.ExceptionError: custom"));
        }
    }
}
