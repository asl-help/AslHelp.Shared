using AslHelp.Options;
using AslHelp.Options.Binding;
using NUnit.Framework;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AslHelp.Tests.Options.Binding;

[TestFixture]
public class ColonKeysTests
{
    [Test]
    public void Expand_ASingleColonKey_BecomesANestedObject()
    {
        var node = JsonNode.Parse("""{ "foo:bar": "0x20" }""");

        ColonKeys.Expand(node);

        Assert.That(node!.ToJsonString(), Is.EqualTo("""{"foo":{"bar":"0x20"}}"""));
    }

    [Test]
    public void Expand_AColonKey_MergesIntoASpelledOutSibling()
    {
        var node = JsonNode.Parse("""{ "chapters": { "label": "Chapters" }, "chapters:enabled": false }""");

        ColonKeys.Expand(node);

        Assert.That(node!.ToJsonString(), Is.EqualTo("""{"chapters":{"label":"Chapters","enabled":false}}"""));
    }

    [Test]
    public void Expand_TwoSegments_NestTwice()
    {
        var node = JsonNode.Parse("""{ "foo:bar:baz": 32 }""");

        ColonKeys.Expand(node);

        Assert.That(node!.ToJsonString(), Is.EqualTo("""{"foo":{"bar":{"baz":32}}}"""));
    }

    [Test]
    public void Expand_AKeyInsideANestedObject_ExpandsThereToo()
    {
        var node = JsonNode.Parse("""{ "settings": { "Chapter 2:toolTip": "fooltip" } }""");

        ColonKeys.Expand(node);

        Assert.That(node!.ToJsonString(), Is.EqualTo("""{"settings":{"Chapter 2":{"toolTip":"fooltip"}}}"""));
    }

    [Test]
    public void Expand_ALeafSetTwice_Teaches()
    {
        var node = JsonNode.Parse("""{ "chapters": { "enabled": true }, "chapters:enabled": false }""");

        Assert.That(
            () => ColonKeys.Expand(node),
            Throws.InstanceOf<JsonException>().With.Message.Contains("'chapters:enabled'"));
    }

    [Test]
    public void Expand_AKeyReachingThroughAValue_Teaches()
    {
        var node = JsonNode.Parse("""{ "title": "Foo", "title:x": 1 }""");

        Assert.That(
            () => ColonKeys.Expand(node),
            Throws.InstanceOf<JsonException>().With.Message.Contains("'title:x'"));
    }

    [Test]
    public void Expand_AKeyMissingANameAroundTheColon_Teaches()
    {
        var node = JsonNode.Parse("""{ "chapters:": true }""");

        Assert.That(
            () => ColonKeys.Expand(node),
            Throws.InstanceOf<JsonException>().With.Message.Contains("'chapters:'"));
    }
}
