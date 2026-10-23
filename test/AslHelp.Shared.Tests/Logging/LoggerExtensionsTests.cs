using AslHelp.Logging;
using NUnit.Framework;
using System.Linq;

namespace AslHelp.Tests.Logging;

[TestFixture]
public partial class LoggerExtensionsTests
{
    [Test]
    public void LogInformation_BelowASinksMinimum_NeverReachesIt()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Warning);
        logger.Add(sink);

        logger.LogInformation($"chatter");
        logger.LogWarning($"worth knowing");

        string[] expected = ["worth knowing"];

        Assert.That(sink.Lines.Select(l => l.Event.Message), Is.EqualTo(expected));
    }

    [Test]
    public void LogInformation_ASinkTakingEverything_CarriesWhereItWasWrittenFrom()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        logger.LogInformation($"here");

        var e = sink.Lines.Single().Event;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(e.MemberName, Is.EqualTo(nameof(LogInformation_ASinkTakingEverything_CarriesWhereItWasWrittenFrom)));
            Assert.That(e.FilePath, Does.EndWith("LoggerExtensionsTests.cs"));
            Assert.That(e.LineNumber, Is.GreaterThan(0));
        }
    }

    [Test]
    public void LogTrace_WithNoSinkThatWould_ComposesNothing()
    {
        using Logger logger = new();
        logger.Add(new RecordingSink(LogLevel.Information));

        Counted value = new();
        logger.LogTrace($"read {value}");

        Assert.That(value.Renders, Is.Zero);
    }

    [Test]
    public void LogTrace_WithASinkThatWould_ComposesTheMessage()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        Counted value = new();
        logger.LogTrace($"read {value}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.Renders, Is.EqualTo(1));
            Assert.That(sink.Lines[0].Event.Message, Is.EqualTo("read counted"));
        }
    }

    [Test]
    public void LogTrace_WithAnAddress_RendersItInHex()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        nint address = unchecked((nint)0x7FF612345678L);
        nuint unsigned = unchecked((nuint)0x7FF612345678UL);

        logger.LogTrace($"at 0x{address:X} and 0x{unsigned:X}");

        // nint isn't IFormattable on .NET Framework 4.8.1, where the generic overload would render it in decimal.
        Assert.That(sink.Lines.Single().Event.Message, Is.EqualTo("at 0x7FF612345678 and 0x7FF612345678"));
    }

    [Test]
    public void Log_AnAddressAtEveryLevel_RendersItInHexAtAllOfThem()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        nint address = unchecked((nint)0x7FF612345678L);

        // Each level goes through a separate handler, so every level is checked for rendering the address in decimal.
        logger.LogDebug($"at 0x{address:X}");
        logger.LogInformation($"at 0x{address:X}");
        logger.LogWarning($"at 0x{address:X}");
        logger.LogError($"at 0x{address:X}");

        Assert.That(sink.Lines.Select(l => l.Event.Message), Is.All.EqualTo("at 0x7FF612345678"));
    }

    [Test]
    public void Log_WithAStringThatAlreadyExists_WritesIt()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        string message = "already a string";
        logger.Log(LogLevel.Debug, message);

        Assert.That(sink.Lines.Single().Event.Message, Is.EqualTo(message));
    }

    [Test]
    public void BeginScope_LinesWrittenInsideIt_AreIndentedUntilItIsDisposed()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Trace);
        logger.Add(sink);

        logger.LogInformation($"outer");

        using (logger.BeginScope(LogLevel.Information, "header"))
        {
            logger.LogInformation($"inner");
        }

        logger.LogInformation($"after");

        int[] depths = [0, 0, 1, 0];

        Assert.That(sink.Lines.Select(l => l.Indent), Is.EqualTo(depths));
    }

    [Test]
    public void BeginScope_WithAHeaderNoSinkWrote_IndentsNothing()
    {
        using Logger logger = new();
        RecordingSink sink = new(LogLevel.Warning);
        logger.Add(sink);

        using (logger.BeginScope(LogLevel.Debug, "a header no sink writes"))
        {
            logger.LogWarning($"careful");
        }

        logger.LogWarning($"after");

        int[] depths = [0, 0];

        Assert.That(sink.Lines.Select(l => l.Indent), Is.EqualTo(depths));
    }
}
