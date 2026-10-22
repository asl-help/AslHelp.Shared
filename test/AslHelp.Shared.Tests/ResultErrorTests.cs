using NUnit.Framework;

namespace AslHelp.Tests;

[TestFixture]
public class ResultErrorTests
{
    // ToString prints the error's full type name and its message.
    [Test]
    public void ToString_WithMessage_PrintsFullTypeNameAndMessage()
    {
        TestError error = new("boom");

        Assert.That(error.ToString(), Is.EqualTo("AslHelp.Tests.TestError: boom"));
    }
}
