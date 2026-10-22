using AslHelp.Logging;
using NUnit.Framework;

namespace AslHelp.Tests.Logging;

[TestFixture]
public class LogFormattersTests
{
    private static LogEvent Event(string message)
    {
        return new(LogLevel.Debug, "asl-help", message, "Attach", "src/UnityPluginBase.cs", 127);
    }

    [Test]
    public void Plain_AtTheTopLevel_IndentsNothing()
    {
        Assert.That(LogFormatters.Plain(Event("attaching"), 0), Is.EqualTo("[dbug] [asl-help] attaching"));
    }

    [Test]
    public void Plain_InsideAScope_MarksTheLineAsAStepUnderIt()
    {
        Assert.That(LogFormatters.Plain(Event("found it"), 1), Is.EqualTo("[dbug] [asl-help]   => found it"));
    }

    [Test]
    public void Plain_DeeperStill_IndentsPerScopeAndKeepsOneArrow()
    {
        Assert.That(LogFormatters.Plain(Event("deeper"), 3), Is.EqualTo("[dbug] [asl-help]       => deeper"));
    }

    [Test]
    public void Plain_WithoutAPrefix_LeavesItOut()
    {
        LogEvent e = new(LogLevel.Warning, "", "careful", "", "", 0);

        Assert.That(LogFormatters.Plain(in e, 0), Is.EqualTo("[warn] careful"));
    }

    [Test]
    public void Stamped_AnIndentedEvent_CarriesTheLevelPrefixAndIndentButNotTheLocation()
    {
        string line = LogFormatters.Stamped(Event("found it"), 1);

        Assert.That(line, Does.EndWith("[dbug] [asl-help]   => found it"));
    }

    [Test]
    public void Located_AnIndentedEvent_SaysTheLocationWithoutStampingTheLine()
    {
        string line = LogFormatters.Located(Event("found it"), 1);

        Assert.That(line, Is.EqualTo("[dbug] [asl-help]   => found it (Attach @ UnityPluginBase.cs:127)"));
    }

    [Test]
    public void Traced_AnIndentedEvent_SaysTheMemberFileAndLine()
    {
        string line = LogFormatters.Traced(Event("found it"), 1);

        Assert.That(line, Does.EndWith("[dbug] [asl-help]   => found it (Attach @ UnityPluginBase.cs:127)"));
    }

    [Test]
    public void Traced_ForALineTheScriptWrote_SaysTheActionAndScript()
    {
        LogEvent e = new(LogLevel.Information, "Fixture", "hello", "startup", "scripts/Script.asl", 42);

        Assert.That(LogFormatters.Traced(in e, 0), Does.EndWith("[info] [Fixture] hello (startup @ Script.asl:42)"));
    }

    [Test]
    public void Traced_WithoutALine_SaysTheScriptAlone()
    {
        LogEvent e = new(LogLevel.Information, "Fixture", "hello", "startup", "scripts/Script.asl", 0);

        Assert.That(LogFormatters.Traced(in e, 0), Does.EndWith("[info] [Fixture] hello (startup @ Script.asl)"));
    }

    [Test]
    public void Traced_WithoutAMemberName_LeavesTheLocationOut()
    {
        LogEvent e = new(LogLevel.Information, "asl-help", "no caller", "", "", 0);

        Assert.That(LogFormatters.Traced(in e, 0), Does.EndWith("[info] [asl-help] no caller"));
    }
}
