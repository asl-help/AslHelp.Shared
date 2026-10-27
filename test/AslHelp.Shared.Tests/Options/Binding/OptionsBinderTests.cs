using AslHelp.Options;
using AslHelp.Options.Binding;
using NUnit.Framework;
using System.Linq;
using System.Text.Json;

using static AslHelp.Tests.Options.Binding.OptionsFixtures;

namespace AslHelp.Tests.Options.Binding;

[TestFixture]
public partial class OptionsBinderTests
{
    [Test]
    public void EnumerateEmbeddedProfiles_TheEmbeddedFolder_YieldsEachDocumentCalledWithoutItsExtension()
    {
        var embedded = OptionsBinder.EnumerateEmbeddedProfiles(typeof(OptionsBinderTests).Assembly, "Options/Binding/Documents")
            .ToDictionary(static d => d.Name, static d => d.Contents);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(embedded.Keys, Is.EquivalentTo((string[])["alpha", "beta"]));
            Assert.That(embedded["alpha"], Does.Contain("0x10"));
        }
    }

    [Test]
    public void ParseProfile_ADocument_BindsItAndHandsItToTheCheckAsTheProfile()
    {
        string? origin = null;

        var profile = OptionsBinder.ParseProfile<FixtureLeaf>(
            "{ \"$schema\": \"x\", \"anything\": \"0x10\" }",
            (parsed, from) => origin = from,
            "$schema");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(profile.Anything, Is.EqualTo(0x10));
            Assert.That(origin, Is.EqualTo("The profile"));
        }
    }

    [Test]
    public void ParseProfile_ANullDocument_ThrowsSayingItIsEmpty()
    {
        Assert.That(
            () => OptionsBinder.ParseProfile<FixtureLeaf>("null", static (_, _) => { }),
            Throws.TypeOf<JsonException>().With.Message.EqualTo("The profile is empty."));
    }

    [Test]
    public void ParseProfile_ADocumentTheCheckRefuses_ThrowsWhatTheCheckThrew()
    {
        Assert.That(
            () => OptionsBinder.ParseProfile<FixtureLeaf>("{ }", static (_, from) => throw new JsonException($"{from} declares no 'anything'.")),
            Throws.TypeOf<JsonException>().With.Message.EqualTo("The profile declares no 'anything'."));
    }

    [Test]
    public void Options_TheSharedGrammar_IsFrozenBeforeItIsShared()
    {
        Assert.That(OptionsBinder.Options.IsReadOnly, Is.True);
    }

    [Test]
    public void Options_ADocument_ReadsCommentsAndTrailingCommas()
    {
        var document = JsonSerializer.Deserialize<JsonElement>(
            "// jsonc\n{ \"value\": 3, }", OptionsBinder.Options);

        Assert.That(document.GetProperty("value").GetInt32(), Is.EqualTo(3));
    }

    [Test]
    public void EnumerateEmbeddedProfiles_AResourceThatIsNotADocument_IsNotYielded()
    {
        var embedded = OptionsBinder
            .EnumerateEmbeddedProfiles(typeof(OptionsBinderTests).Assembly, "Options/Binding/Documents")
            .Select(entry => entry.Name);

        Assert.That(embedded, Has.None.Contains("readme"));
    }

    [Test]
    public void ApplyOverrides_ALeafSetInTheOverrides_Wins()
    {
        var widget = CreateFull();

        OptionsBinder.ApplyOverrides(widget, new FixtureWidget { Kind = "alpha", Size = null, Count = null });

        Assert.That(widget.Kind, Is.EqualTo("alpha"));
    }

    // Null means the standing value stays, so an override can adjust one leaf without repeating the rest.
    [Test]
    public void ApplyOverrides_ALeafTheOverridesLeaveNull_KeepsTheStandingValue()
    {
        var widget = CreateFull();

        OptionsBinder.ApplyOverrides(widget, new FixtureWidget { Kind = "alpha", Size = null, Count = null });

        Assert.That(widget.Size, Is.EqualTo(0x20));
    }

    [Test]
    public void ApplyOverrides_ASection_RecursesIntoItsLeaves()
    {
        var widget = CreateFull();

        OptionsBinder.ApplyOverrides(
            widget,
            new FixtureWidget { Size = null, Count = null, Parts = { Width = 0x4D0, Name = null } });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(widget.Parts.Width, Is.EqualTo(0x4D0));
            Assert.That(widget.Parts.Name, Is.EqualTo(0x48));
        }
    }

    [Test]
    public void ApplyOverrides_AnIncludedField_IsCopiedAndAMemberLeftOutIsNot()
    {
        FixtureMembers target = new() { IncludedField = 1 };
        FixtureMembers overrides = new() { IncludedField = 2, Ignored = "never", PlainField = 3 };

        OptionsBinder.ApplyOverrides(target, overrides);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(target.IncludedField, Is.EqualTo(2));
            Assert.That(target.Ignored, Is.Null);
            Assert.That(target.PlainField, Is.Null);
        }
    }

    [Test]
    public void ApplyOverrides_WithNoOverrides_ThrowsForTheArgument()
    {
        Assert.That(() => OptionsBinder.ApplyOverrides(CreateFull(), null!), Throws.ArgumentNullException);
    }
}
