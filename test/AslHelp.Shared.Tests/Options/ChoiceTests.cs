using AslHelp.Options;
using NUnit.Framework;
using System.Text.Json;

namespace AslHelp.Tests.Options;

public class ChoiceTests
{
    private enum Kind
    {
        FField,
        UProperty,
        InlineNul,
    }

    private enum Aliased
    {
        [ChoiceAlias("a")]
        First,

        Second,
    }

    [Test]
    public void Spellings_AMemberOpeningWithTwoCapitals_KeepsThemTogether()
    {
        Assert.That(Choice.Spellings<Kind>(), Is.EqualTo(["ffield", "uproperty", "inline-nul"]));
    }

    [Test]
    public void Parse_ASpelling_FindsItsMember()
    {
        Assert.That(Choice.Parse<Kind>("uproperty", "The profile", "fieldShape"), Is.EqualTo(Kind.UProperty));
    }

    [Test]
    public void Parse_AnUnknownSpelling_ThrowsSayingTheSpellings()
    {
        var thrown = Assert.Throws<JsonException>(() => Choice.Parse<Kind>("f-field", "The profile", "fieldShape"));

        Assert.That(thrown!.Message, Does.Contain("fieldShape").And.Contain("\"ffield\", \"uproperty\" or \"inline-nul\""));
    }

    [Test]
    public void Parse_AMembersOwnAlias_FindsThatMember()
    {
        Assert.That(Choice.Parse<Aliased>("a", "The profile", "aliased"), Is.EqualTo(Aliased.First));
    }

    [Test]
    public void Aliases_ListsOnlyTheMembersThatRegisterOne()
    {
        Assert.That(Choice.Aliases<Aliased>(), Is.EqualTo(["a"]));
    }

    // A log level parses from the same tag a formatted line shows.
    [Test]
    public void Parse_ALogLevelTag_ReturnsTheLevel()
    {
        Assert.That(Choice.Parse<AslHelp.Logging.LogLevel>("info", "options", "level"), Is.EqualTo(AslHelp.Logging.LogLevel.Information));
    }
}
