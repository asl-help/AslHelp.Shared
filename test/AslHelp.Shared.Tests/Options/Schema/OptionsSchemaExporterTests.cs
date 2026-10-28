using AslHelp.Options;
using AslHelp.Options.Schema;
using NUnit.Framework;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AslHelp.Tests.Options.Schema;

// The schema comes off the serializer's contract, plus the few facts about a value that the contract lacks.
[TestFixture]
public partial class OptionsSchemaExporterTests
{
    // The schema files escape nothing past what JSON demands, so a `+` in a pattern reads the same here.
    private static readonly JsonSerializerOptions _relaxed = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static JsonObject Properties(bool allowColonKeys = false)
    {
        return OptionsSchemaExporter.Export(typeof(FixtureOptions), "Fixture", allowColonKeys)["properties"]!.AsObject();
    }

    [Test]
    public void Export_AnOptionsType_OpensWithTheDraftAndTitle()
    {
        var schema = OptionsSchemaExporter.Export(typeof(FixtureOptions), "Fixture", allowColonKeys: false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(schema["$schema"]!.GetValue<string>(), Is.EqualTo("https://json-schema.org/draft/2020-12/schema"));
            Assert.That(schema["title"]!.GetValue<string>(), Is.EqualTo("Fixture"));
            Assert.That(schema["additionalProperties"]!.GetValue<bool>(), Is.False);
        }
    }

    // A document at the root may point its editor at the schema, so only the root accepts `$schema`.
    [Test]
    public void Export_TheRoot_AcceptsTheSchemaPointer()
    {
        var properties = Properties();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(properties.First().Key, Is.EqualTo("$schema"));
            Assert.That(properties["$schema"]!["type"]!.GetValue<string>(), Is.EqualTo("string"));
            Assert.That(properties.Where(static p => p.Value?["properties"] is JsonObject section && section.ContainsKey("$schema")), Is.Empty);
        }
    }

    // A member's description survives the node a check or a hex number writes in place of the exporter's own.
    [Test]
    public void Export_ADescribedMember_KeepsItsDescription()
    {
        var properties = Properties();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(properties["kept"]!["description"]!.GetValue<string>(), Is.EqualTo("A plain value."));
            Assert.That(properties["count"]!["description"]!.GetValue<string>(), Is.EqualTo("A whole number."));
            Assert.That(
                properties["level"]!["description"]!.GetValue<string>(),
                Is.EqualTo("The lowest level. A spelling matches ignoring case."));
        }
    }

    // The keys are the members the serializer binds, under the keys it binds them by.
    [Test]
    public void Export_TheKeys_AreTheOnesTheSerializerBinds()
    {
        var properties = Properties();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(properties.ContainsKey("kept"), Is.True);
            Assert.That(properties.ContainsKey("label"), Is.True);
            Assert.That(properties.ContainsKey("ignored"), Is.False);
            Assert.That(properties.ContainsKey("name"), Is.False);
        }
    }

    // A whole number binds through the hex converter, so it reads as a JSON number or as hex text.
    [Test]
    public void Export_AWholeNumber_ReadsAsANumberOrHexText()
    {
        Assert.That(Properties()["count"]!.ToJsonString(_relaxed), Does.Contain("integer").And.Contain("^0x[a-fA-F0-9]+$"));
    }

    // A list or a map of whole numbers binds each one through the hex converter as well.
    [Test]
    public void Export_AListOrAMapOfWholeNumbers_ReadsEachAsANumberOrHexText()
    {
        var properties = Properties();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(properties["sizes"]!["items"]!.ToJsonString(_relaxed), Does.Contain("^0x[a-fA-F0-9]+$"));
            Assert.That(properties["fields"]!["additionalProperties"]!.ToJsonString(_relaxed), Does.Contain("^0x[a-fA-F0-9]+$"));
        }
    }

    // The binder routes any key an object with extension data does not declare there, so the schema takes it too.
    [Test]
    public void Export_AnObjectWithExtensionData_TakesAnyKey()
    {
        Assert.That(Properties()["open"]!["additionalProperties"]!.GetValue<bool>(), Is.True);
    }

    [Test]
    public void Export_AChoice_ListsItsSpellingsAndAliases()
    {
        var level = Properties()["level"]!;

        Assert.That(level["enum"]!.AsArray().Select(static spelling => spelling!.GetValue<string>()), Is.EqualTo(["trace", "debug", "information", "warning", "error", "off", "trce", "dbug", "info", "warn", "fail"]));
    }

    // The converter that enforces a value's grammar describes it, so the schema and the binder agree.
    [Test]
    public void Export_AValueBoundByAConverterThatDescribesItself_TakesTheConvertersSchema()
    {
        Assert.That(Properties()["version"]!.ToJsonString(_relaxed), Is.EqualTo("""{"type":"string","pattern":"^[0-9]+"}"""));
    }

    [Test]
    public void Export_ATypeThatDescribesItself_TakesItsOwnSchema()
    {
        Assert.That(Properties()["self"]!.ToJsonString(_relaxed), Is.EqualTo("""{"type":"array"}"""));
    }

    [Test]
    public void Export_ATypeWithRules_AddsThemToItsObject()
    {
        Assert.That(Properties()["ruled"]!["minProperties"]!.GetValue<int>(), Is.EqualTo(1));
    }

    // The loader expands a colon spelling in every object of an options file, so every object takes it.
    [Test]
    public void Export_WithColonKeys_LetsEveryObjectTakeThem()
    {
        var properties = Properties(allowColonKeys: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(properties["ruled"]!["patternProperties"], Is.Not.Null);
            Assert.That(properties["nodes"]!["additionalProperties"]!["patternProperties"], Is.Not.Null);
            Assert.That(Properties()["ruled"]!["patternProperties"], Is.Null);
        }
    }

    // A type that contains itself refers back to where it was first written, which already has everything below it.
    [Test]
    public void Export_ATypeContainingItself_RefersBackWithoutAddingToTheReference()
    {
        var children = Properties(allowColonKeys: true)["nodes"]!["additionalProperties"]!["properties"]!["children"]!["additionalProperties"]!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(children["$ref"]!.GetValue<string>(), Is.EqualTo("#/properties/nodes/additionalProperties"));
            Assert.That(children["patternProperties"], Is.Null);
        }
    }

    [Test]
    public void Export_WithNoType_ThrowsSayingTheArgument()
    {
        Assert.That(
            () => OptionsSchemaExporter.Export(null!, "Fixture", allowColonKeys: false),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("type"));
    }
}
