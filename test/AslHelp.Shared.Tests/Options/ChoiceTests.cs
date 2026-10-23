using AslHelp.Options;
using NUnit.Framework;
using System.Text.Json;

namespace AslHelp.Tests.Options;

public class ChoiceTests
{
    private enum Kind
    {
        FFoo,
        UBar,
        InlineBaz,
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
        Assert.That(Choice.Spellings<Kind>(), Is.EqualTo(["ffoo", "ubar", "inline-baz"]));
    }

    [Test]
    public void Parse_ASpelling_FindsItsMember()
    {
        Assert.That(Choice.Parse<Kind>("ubar", "The fixture", "kind"), Is.EqualTo(Kind.UBar));
    }

    [Test]
    public void Parse_AnUnknownSpelling_ThrowsSayingTheSpellings()
    {
        var thrown = Assert.Throws<JsonException>(() => Choice.Parse<Kind>("f-foo", "The fixture", "kind"));

        Assert.That(thrown!.Message, Does.Contain("kind").And.Contain("\"ffoo\", \"ubar\" or \"inline-baz\""));
    }

    [Test]
    public void Parse_AMembersOwnAlias_FindsThatMember()
    {
        Assert.That(Choice.Parse<Aliased>("a", "The fixture", "aliased"), Is.EqualTo(Aliased.First));
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
