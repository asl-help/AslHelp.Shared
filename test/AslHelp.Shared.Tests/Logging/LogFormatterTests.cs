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
    public void Plain_AtTheTopLevel_IndentsNothing()
    {
        Assert.That(LogFormatter.Plain(Event("attaching"), 0), Is.EqualTo("[dbug] [asl-help] attaching"));
    }

    [Test]
    public void Plain_InsideAScope_MarksTheLineAsAStepUnderIt()
    {
        Assert.That(LogFormatter.Plain(Event("found it"), 1), Is.EqualTo("[dbug] [asl-help]   => found it"));
    }

    [Test]
    public void Plain_DeeperStill_IndentsPerScopeAndKeepsOneArrow()
    {
        Assert.That(LogFormatter.Plain(Event("deeper"), 3), Is.EqualTo("[dbug] [asl-help]       => deeper"));
    }

    [Test]
    public void Plain_WithoutAPrefix_LeavesItOut()
    {
        LogEvent e = new(LogLevel.Warning, "", "careful", "", "", 0);

        Assert.That(LogFormatter.Plain(in e, 0), Is.EqualTo("[warn] careful"));
    }

    [Test]
    public void Stamped_AnIndentedEvent_CarriesTheLevelPrefixAndIndentButNotTheLocation()
    {
        string line = LogFormatter.Stamped(Event("found it"), 1);

        Assert.That(line, Does.EndWith("[dbug] [asl-help]   => found it"));
    }

    [Test]
    public void Located_AnIndentedEvent_SaysTheLocationWithoutStampingTheLine()
    {
        string line = LogFormatter.Located(Event("found it"), 1);

        Assert.That(line, Is.EqualTo("[dbug] [asl-help]   => found it (Attach @ UnityPluginBase.cs:127)"));
    }

    [Test]
    public void Traced_AnIndentedEvent_SaysTheMemberFileAndLine()
    {
        string line = LogFormatter.Traced(Event("found it"), 1);

        Assert.That(line, Does.EndWith("[dbug] [asl-help]   => found it (Attach @ UnityPluginBase.cs:127)"));
    }

    [Test]
    public void Traced_ForALineTheScriptWrote_SaysTheActionAndScript()
    {
        LogEvent e = new(LogLevel.Information, "Fixture", "hello", "startup", "scripts/Script.asl", 42);

        Assert.That(LogFormatter.Traced(in e, 0), Does.EndWith("[info] [Fixture] hello (startup @ Script.asl:42)"));
    }

    [Test]
    public void Traced_WithoutALine_SaysTheScriptAlone()
    {
        LogEvent e = new(LogLevel.Information, "Fixture", "hello", "startup", "scripts/Script.asl", 0);

        Assert.That(LogFormatter.Traced(in e, 0), Does.EndWith("[info] [Fixture] hello (startup @ Script.asl)"));
    }

    [Test]
    public void Traced_WithoutAMemberName_LeavesTheLocationOut()
    {
        LogEvent e = new(LogLevel.Information, "asl-help", "no caller", "", "", 0);

        Assert.That(LogFormatter.Traced(in e, 0), Does.EndWith("[info] [asl-help] no caller"));
    }

    // Only a sink at trace gets the location, with or without the timestamp.
    [TestCase(LogLevel.Trace, true, "Traced")]
    [TestCase(LogLevel.Trace, false, "Located")]
    [TestCase(LogLevel.Debug, true, "Stamped")]
    [TestCase(LogLevel.Error, false, "Plain")]
    public void ForLevel_AMinimumLevel_PicksTheFormatter(LogLevel minimumLevel, bool stamped, string expected)
    {
        Assert.That(LogFormatter.ForLevel(minimumLevel, stamped).Method.Name, Is.EqualTo(expected));
    }
}
