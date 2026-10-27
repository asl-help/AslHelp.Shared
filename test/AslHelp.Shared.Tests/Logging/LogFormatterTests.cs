using AslHelp.Logging;
using NUnit.Framework;

namespace AslHelp.Tests.Logging;

[TestFixture]
public class LogFormatterTests
{
    private static LogEvent Event(string message)
    {
        return new(LogLevel.Debug, "asl-help", message, "Attach", "src/UnityPluginBase.cs", 127);
    }

    [Test]
    public void FormatPlain_AtTheTopLevel_IndentsNothing()
    {
        Assert.That(LogFormatter.FormatPlain(Event("attaching"), 0), Is.EqualTo("[dbug] [asl-help] attaching"));
    }

    [Test]
    public void FormatPlain_InsideAScope_MarksTheLineAsAStepUnderIt()
    {
        Assert.That(LogFormatter.FormatPlain(Event("found it"), 1), Is.EqualTo("[dbug] [asl-help]   => found it"));
    }

    [Test]
    public void FormatPlain_DeeperStill_IndentsPerScopeAndKeepsOneArrow()
    {
        Assert.That(LogFormatter.FormatPlain(Event("deeper"), 3), Is.EqualTo("[dbug] [asl-help]       => deeper"));
    }

    [Test]
    public void FormatPlain_WithoutAPrefix_LeavesItOut()
    {
        LogEvent e = new(LogLevel.Warning, "", "careful", "", "", 0);

        Assert.That(LogFormatter.FormatPlain(in e, 0), Is.EqualTo("[warn] careful"));
    }

    [Test]
    public void FormatWithTimestamp_AnIndentedEvent_WritesTheLevelPrefixAndIndentWithoutTheLocation()
    {
        string line = LogFormatter.FormatWithTimestamp(Event("found it"), 1);

        Assert.That(line, Does.EndWith("[dbug] [asl-help]   => found it"));
    }

    [Test]
    public void FormatWithLocation_AnIndentedEvent_WritesTheLocationWithoutATimestamp()
    {
        string line = LogFormatter.FormatWithLocation(Event("found it"), 1);

        Assert.That(line, Is.EqualTo("[dbug] [asl-help]   => found it (Attach @ UnityPluginBase.cs:127)"));
    }

    [Test]
    public void FormatWithTimestampAndLocation_AnIndentedEvent_WritesTheMemberFileAndLine()
    {
        string line = LogFormatter.FormatWithTimestampAndLocation(Event("found it"), 1);

        Assert.That(line, Does.EndWith("[dbug] [asl-help]   => found it (Attach @ UnityPluginBase.cs:127)"));
    }

    [Test]
    public void FormatWithTimestampAndLocation_ForALineTheScriptWrote_WritesTheActionAndScript()
    {
        LogEvent e = new(LogLevel.Information, "Fixture", "hello", "startup", "scripts/Script.asl", 42);

        Assert.That(LogFormatter.FormatWithTimestampAndLocation(in e, 0), Does.EndWith("[info] [Fixture] hello (startup @ Script.asl:42)"));
    }

    [Test]
    public void FormatWithTimestampAndLocation_WithoutALine_WritesTheScriptAlone()
    {
        LogEvent e = new(LogLevel.Information, "Fixture", "hello", "startup", "scripts/Script.asl", 0);

        Assert.That(LogFormatter.FormatWithTimestampAndLocation(in e, 0), Does.EndWith("[info] [Fixture] hello (startup @ Script.asl)"));
    }

    [Test]
    public void FormatWithTimestampAndLocation_WithoutAMemberName_LeavesTheLocationOut()
    {
        LogEvent e = new(LogLevel.Information, "asl-help", "no caller", "", "", 0);

        Assert.That(LogFormatter.FormatWithTimestampAndLocation(in e, 0), Does.EndWith("[info] [asl-help] no caller"));
    }

    // Only a sink at trace gets the location, with or without the timestamp.
    [TestCase(LogLevel.Trace, true, "FormatWithTimestampAndLocation")]
    [TestCase(LogLevel.Trace, false, "FormatWithLocation")]
    [TestCase(LogLevel.Debug, true, "FormatWithTimestamp")]
    [TestCase(LogLevel.Error, false, "FormatPlain")]
    public void Create_AMinimumLevel_PicksTheFormatter(LogLevel minimumLevel, bool includeTimestamp, string expected)
    {
        Assert.That(LogFormatter.Create(minimumLevel, includeTimestamp).Method.Name, Is.EqualTo(expected));
    }
}
