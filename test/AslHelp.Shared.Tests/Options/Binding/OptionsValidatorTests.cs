using AslHelp.Options;
using AslHelp.Options.Binding;
using AslHelp.Options.Capabilities;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using static AslHelp.Tests.Options.Binding.OptionsFixtures;

namespace AslHelp.Tests.Options.Binding;

[TestFixture]
public class OptionsValidatorTests
{
    private static Dictionary<string, UnavailableCapability> Unavailable(string capability, string state)
    {
        return new() { [capability] = new() { State = state, Reason = "the build keeps no such thing" } };
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    [Test]
    public void ThrowIfMissingRequired_AWidgetWithEveryRequiredValue_Passes()
    {
        Assert.That(() => OptionsValidator.ThrowIfMissingRequired(CreateFull(), "The fixture"), Throws.Nothing);
    }

    // A missing required leaf is reported by its full path. It would otherwise surface deep inside the first read,
    // looking like a target that isn't ready.
    [Test]
    public void ThrowIfMissingRequired_AMissingRequiredLeaf_ReportsItsFullPath()
    {
        var widget = CreateFull();
        widget.Parts.Width = null;

        var found = Assert.Throws<JsonException>(() => OptionsValidator.ThrowIfMissingRequired(widget, "The fixture"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found!.Message, Does.Contain("The fixture"));
            Assert.That(found!.Message, Does.Contain("parts.full_width"));
        }
    }

    [Test]
    public void ThrowIfMissingRequired_ALeafItsDecidingOptionCallsFor_ReportsItsFullPath()
    {
        var widget = CreateFull();
        widget.Parts.Depth = null;

        Assert.That(
            () => OptionsValidator.ThrowIfMissingRequired(widget, "The fixture"),
            Throws.TypeOf<JsonException>().With.Message.EqualTo("The fixture declares no 'parts.full_depth'."));
    }

    [TestCase("alpha")]
    [TestCase(null)]
    public void ThrowIfMissingRequired_ALeafItsDecidingOptionDoesNotCallFor_Passes(string? kind)
    {
        var widget = CreateFull();
        widget.Kind = kind;
        widget.Parts.Depth = null;

        Assert.That(() => OptionsValidator.ThrowIfMissingRequired(widget, "The fixture"), Throws.Nothing);
    }

    [Test]
    public void ThrowIfMissingRequired_ADecidingValueInAnotherCase_StillCallsForTheLeaf()
    {
        var widget = CreateFull();
        widget.Kind = "BETA";
        widget.Parts.Depth = null;

        Assert.That(() => OptionsValidator.ThrowIfMissingRequired(widget, "The fixture"), Throws.TypeOf<JsonException>());
    }

    [Test]
    public void ThrowIfMissingRequired_ACapabilityDeclaredUnavailable_PassesOverItsLeaves()
    {
        var widget = CreateFull();
        widget.Parts.Width = null;

        Assert.That(() => OptionsValidator.ThrowIfMissingRequired(widget, "The fixture", Unavailable("draw", "not-applicable")), Throws.Nothing);
    }

    [Test]
    public void ThrowIfMissingRequired_ALeafOfSeveralCapabilities_IsPassedOverOnlyWhenEveryOneIsUnavailable()
    {
        var widget = CreateFull();
        widget.Count = null;

        var both = Unavailable("draw", "not-applicable");
        both["save"] = new() { State = "not-applicable", Reason = "the build keeps no such thing" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => OptionsValidator.ThrowIfMissingRequired(widget, "The fixture", Unavailable("draw", "not-applicable")),
                Throws.TypeOf<JsonException>().With.Message.Contains("'count'"));
            Assert.That(() => OptionsValidator.ThrowIfMissingRequired(widget, "The fixture", both), Throws.Nothing);
        }
    }

    [Test]
    public void ThrowIfUnavailable_AnUnavailableCapability_RefusesTheSurfaceWithTheReason()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => UnavailableCapability.ThrowIfUnavailable(Unavailable("draw", "not-applicable"), "draw", "drawing"),
                Throws.InvalidOperationException.With.Message.EqualTo(
                    "The options declare the drawing unavailable: the build keeps no such thing"));
            Assert.That(() => UnavailableCapability.ThrowIfUnavailable(Unavailable("draw", "not-applicable"), "save", "save"), Throws.Nothing);
            Assert.That(() => UnavailableCapability.ThrowIfUnavailable(null, "draw", "drawing"), Throws.Nothing);
            Assert.That(UnavailableCapability.Validate(Unavailable("draw", "not-applicable"), "draw", "drawing").Error?.Message, Does.Contain("drawing"));
            Assert.That(UnavailableCapability.Validate(null, "draw", "drawing").IsOk, Is.True);
        }
    }

    [Test]
    public void ThrowIfMissingRequired_ALeafOutsideTheUnavailableCapability_IsStillRequired()
    {
        var widget = CreateFull();
        widget.Size = null;

        Assert.That(
            () => OptionsValidator.ThrowIfMissingRequired(widget, "The fixture", Unavailable("draw", "not-applicable")),
            Throws.TypeOf<JsonException>().With.Message.Contains("'size'"));
    }

    [Test]
    public void ThrowIfMissingRequired_ACapabilityTheOptionsDoNotMark_ListsTheOnesThatExist()
    {
        Assert.That(
            () => OptionsValidator.ThrowIfMissingRequired(CreateFull(), "The fixture", Unavailable("print", "not-applicable")),
            Throws.TypeOf<JsonException>().With.Message.Contains("'print'").And.Message.Contains("draw"));
    }

    [Test]
    public void ThrowIfMissingRequired_AnUnavailableCapabilityInAnUnknownState_ListsTheStates()
    {
        Assert.That(
            () => OptionsValidator.ThrowIfMissingRequired(CreateFull(), "The fixture", Unavailable("draw", "missing")),
            Throws.TypeOf<JsonException>().With.Message.Contains("'missing'").And.Message.Contains("unresolved"));
    }

    [Test]
    public void ThrowIfMissingRequired_AnUnavailableCapabilityWithoutAReason_ReportsItsPath()
    {
        Dictionary<string, UnavailableCapability> unavailable = new() { ["draw"] = new() { State = "unresolved" } };

        Assert.That(
            () => OptionsValidator.ThrowIfMissingRequired(CreateFull(), "The fixture", unavailable),
            Throws.TypeOf<JsonException>().With.Message.Contains("unavailable.draw.reason"));
    }

    // A `null` under `unavailable` is a one-line typo, so the error gives its path rather than a bare null reference.
    [Test]
    public void ThrowIfMissingRequired_AnUnavailableCapabilityThatIsNull_ReportsItsPath()
    {
        Dictionary<string, UnavailableCapability> unavailable = new() { ["draw"] = null! };

        Assert.That(
            () => OptionsValidator.ThrowIfMissingRequired(CreateFull(), "The fixture", unavailable),
            Throws.TypeOf<JsonException>().With.Message.Contains("unavailable.draw"));
    }

    [Test]
    public void ThrowIfMissingRequired_AMissingOptionalLeaf_IsNotMissingAnything()
    {
        var widget = CreateFull();
        widget.Parts.Name = null;

        Assert.That(() => OptionsValidator.ThrowIfMissingRequired(widget, "The fixture"), Throws.Nothing);
    }

    [Test]
    public void GetUnsetKeys_AWidgetWithEveryRequiredValue_ListsNothing()
    {
        Assert.That(OptionsValidator.GetUnsetKeys(CreateFull()), Is.Empty);
    }

    // Lists every leaf a widget is missing rather than only the first, so a writer can fill in each one. The list
    // leaves out an optional leaf and a leaf that its deciding option does not require.
    [Test]
    public void GetUnsetKeys_SeveralMissingLeaves_ListsEveryRequiredOneByItsFullPath()
    {
        var widget = CreateFull();
        widget.Size = null;
        widget.Parts.Width = null;
        widget.Parts.Name = null;
        widget.Parts.Depth = null;

        Assert.That(OptionsValidator.GetUnsetKeys(widget), Is.EqualTo(["size", "parts.full_width", "parts.full_depth"]));
    }

    [Test]
    public void GetUnsetKeys_ALeafItsDecidingOptionDoesNotCallFor_IsNotListed()
    {
        var widget = CreateFull();
        widget.Kind = "alpha";
        widget.Parts.Depth = null;

        Assert.That(OptionsValidator.GetUnsetKeys(widget), Is.Empty);
    }

    // The keys a document may use are exactly the members the serializer binds, so every key accepted here binds.
    [Test]
    public void ValidateKeys_EveryKeyTheSerializerBinds_Passes()
    {
        string json = """
            { "title": "Game", "label": "n", "includedField": 1, "parts": { "full_width": 2, "name": 3 }, "leaf": { "anything": 4 } }
            """;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => OptionsValidator.ValidateKeys(Parse(json), typeof(FixtureMembers), OptionsBinder.Options, "options"), Throws.Nothing);
            Assert.That(() => JsonSerializer.Deserialize<FixtureMembers>(json, Strict()), Throws.Nothing);
        }
    }

    // An unknown key is refused with the keys that exist. A typo would otherwise merge silently, the same as never
    // writing the value.
    [TestCase("name")]
    [TestCase("plainField")]
    public void ValidateKeys_AKeyTheSerializerDoesNotBind_FailsWithTheKeysThatExist(string key)
    {
        var element = Parse($$"""{ "{{key}}": null }""");

        var found = Assert.Throws<JsonException>(
            () => OptionsValidator.ValidateKeys(element, typeof(FixtureMembers), OptionsBinder.Options, "options"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found.Message, Does.Contain($"'{key}'"));
            Assert.That(found.Message, Does.Contain("label").And.Contain("includedField").And.Contain("title").And.Contain("parts"));
            Assert.That(() => JsonSerializer.Deserialize<FixtureMembers>(element, Strict()), Throws.InstanceOf<JsonException>());
        }
    }

    // The serializer reads a key that matches an ignored member and drops its value, refusing nothing. The check
    // refuses it, since a value that goes nowhere is the same mistake as a typo.
    [Test]
    public void ValidateKeys_AKeyOfAnIgnoredMember_FailsThoughTheSerializerDropsIt()
    {
        var element = Parse("""{ "ignored": "x" }""");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => OptionsValidator.ValidateKeys(element, typeof(FixtureMembers), OptionsBinder.Options, "options"),
                Throws.InstanceOf<JsonException>().With.Message.Contains("'ignored'"));
            Assert.That(JsonSerializer.Deserialize<FixtureMembers>(element, Strict())!.Ignored, Is.Null);
        }
    }

    [Test]
    public void ValidateKeys_AnUnknownKeyInsideASection_ReportsTheSection()
    {
        var element = Parse("""{ "parts": { "nam": 1 } }""");

        var found = Assert.Throws<JsonException>(
            () => OptionsValidator.ValidateKeys(element, typeof(FixtureMembers), OptionsBinder.Options, "options"));

        Assert.That(found.Message, Does.Contain("'parts'").And.Contain("'nam'").And.Contain("full_width, name"));
    }

    // Every object the serializer binds is checked, inside a list or a map as well as directly below a member.
    [TestCase("""{ "leaf": { "anythng": 1 } }""", "leaf")]
    [TestCase("""{ "leaves": [{ "anything": 1 }, { "anythng": 1 }] }""", "leaves")]
    [TestCase("""{ "named": { "first": { "anythng": 1 } } }""", "first")]
    public void ValidateKeys_AnUnknownKeyInAnObjectTheSerializerBinds_FailsWithTheObject(string json, string owner)
    {
        Assert.That(
            () => OptionsValidator.ValidateKeys(Parse(json), typeof(FixtureMembers), OptionsBinder.Options, "options"),
            Throws.InstanceOf<JsonException>().With.Message.Contains($"'{owner}'").And.Message.Contains("'anythng'"));
    }

    // The serializer routes every key the type does not declare into extension data, so the object accepts any key and
    // the type's validation checks the values that go there.
    [Test]
    public void ValidateKeys_AnObjectWithExtensionData_TakesAnyKey()
    {
        var element = Parse("""{ "retained": "k", "foo:bar": { "x": 1 } }""");

        Assert.That(
            () => OptionsValidator.ValidateKeys(element, typeof(FixtureOpen), OptionsBinder.Options, "options"),
            Throws.Nothing);
    }

    // A document may add keys around the type at its root, such as the schema pin, but nowhere else.
    [Test]
    public void ValidateKeys_AnEnvelopeKey_PassesAtTheRootOnly()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => OptionsValidator.ValidateKeys(Parse("""{ "$schema": "x" }"""), typeof(FixtureMembers), OptionsBinder.Options, "options", "$schema"),
                Throws.Nothing);
            Assert.That(
                () => OptionsValidator.ValidateKeys(Parse("""{ "parts": { "$schema": "x" } }"""), typeof(FixtureMembers), OptionsBinder.Options, "options", "$schema"),
                Throws.InstanceOf<JsonException>().With.Message.Contains("'$schema'"));
        }
    }

    [Test]
    public void ValidateKeys_SomethingOtherThanAnObject_HasNoKeysToCheck()
    {
        Assert.That(
            () => OptionsValidator.ValidateKeys(Parse("[1, 2]"), typeof(FixtureMembers), OptionsBinder.Options, "options"),
            Throws.Nothing);
    }

    [Test]
    public void ValidateKeys_WithNoType_ThrowsForTheArgument()
    {
        Assert.That(
            () => OptionsValidator.ValidateKeys(Parse("{}"), null!, OptionsBinder.Options, "options"),
            Throws.ArgumentNullException.With.Property("ParamName").EqualTo("type"));
    }

    [Test]
    public void ThrowIfMissingRequired_WithNoOptions_ThrowsForTheArgument()
    {
        Assert.That(() => OptionsValidator.ThrowIfMissingRequired(null!, "The fixture"), Throws.ArgumentNullException);
    }

    // The reader decides what an unset option means, so a sparse set of overrides passes as well as a full one.
    [Test]
    public void ValidateValues_MarkedOptionsThatFitOrAreUnset_Pass()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => OptionsValidator.ValidateValues(new FixtureChecked { Level = "info", Gadget = { Color = "green" } }, "Initialize"),
                Throws.Nothing);
            Assert.That(() => OptionsValidator.ValidateValues(new FixtureChecked(), "Initialize"), Throws.Nothing);
        }
    }

    [Test]
    public void ValidateValues_AChoiceThatMatchesNoSpelling_ThrowsWithTheOptionAndTheValue()
    {
        Assert.That(
            () => OptionsValidator.ValidateValues(new FixtureChecked { Level = "verbose" }, "Initialize"),
            Throws.TypeOf<JsonException>().With.Message.Contains("'level'").And.Message.Contains("'verbose'"));
    }

    // A marked option inside a section is checked as well. The error gives its full path.
    [Test]
    public void ValidateValues_AChoiceInASectionThatFitsNone_ThrowsWithItsFullPath()
    {
        Assert.That(
            () => OptionsValidator.ValidateValues(new FixtureChecked { Gadget = { Color = "purple" } }, "Initialize"),
            Throws.TypeOf<JsonException>().With.Message.EqualTo(
                "Initialize declares the unknown 'gadget.color' value 'purple', expected \"red\" or \"green\"."));
    }

    [Test]
    public void ValidateValues_WithNoOptions_ThrowsForTheArgument()
    {
        Assert.That(() => OptionsValidator.ValidateValues(null!, "Initialize"), Throws.ArgumentNullException);
    }

    // Binds the way an options file does, refusing a key no member takes.
    private static JsonSerializerOptions Strict()
    {
        return new(OptionsBinder.Options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    }
}
