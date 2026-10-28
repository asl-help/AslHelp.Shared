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
public class OptionsContractTests
{
    // The keys are the ones the serializer produces: renamed and camel-cased, with the opted-in field included and the
    // ignored member and the plain field left out.
    [Test]
    public void GetMembers_AnOptionsType_ListsWhatTheSerializerBindsUnderItsDocumentKeys()
    {
        var keys = OptionsContract.GetMembers(typeof(FixtureMembers)).Select(static member => member.Key);

        Assert.That(keys, Is.EquivalentTo((string[])["label", "includedField", "parts", "leaf", "leaves", "named", "title"]));
    }

    [Test]
    public void GetMembers_ASectionAndALeaf_AreToldApartWithTheirMarks()
    {
        var members = OptionsContract.GetMembers(typeof(FixtureWidget)).ToDictionary(static member => member.Key);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(members["parts"].IsSection, Is.True);
            Assert.That(members["parts"].Capabilities, Is.EqualTo(["draw"]));
            Assert.That(members["count"].IsSection, Is.False);
            Assert.That(members["count"].IsRequired, Is.True);
            Assert.That(members["count"].Capabilities, Is.EqualTo(["draw", "save"]));
            Assert.That(members["kind"].IsRequired, Is.False);
            Assert.That(members["size"].Type, Is.EqualTo(typeof(int?)));
        }
    }

    [Test]
    public void GetMember_APathThroughASection_FindsTheLeafByItsDocumentKeys()
    {
        var member = OptionsContract.GetMember(typeof(FixtureWidget), "parts", "full_width");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(member.Key, Is.EqualTo("full_width"));
            Assert.That(member.IsRequired, Is.True);
        }
    }

    [Test]
    public void GetMember_AKeyTheTypeDoesNotBind_ThrowsWithTheTypeAndTheKey()
    {
        Assert.That(
            () => OptionsContract.GetMember(typeof(FixtureWidget), "parts", "Width"),
            Throws.InvalidOperationException.With.Message.EqualTo("FixtureParts has no member the document calls 'Width'."));
    }

    [Test]
    public void TryGetMember_AKeyTheTypeDoesNotBind_ReturnsFalse()
    {
        Assert.That(OptionsContract.TryGetMember(typeof(FixtureWidget), "Size", out _), Is.False);
    }

    [Test]
    public void GetValue_APathThroughASection_ReadsTheLeafAndSetWritesIt()
    {
        FixtureWidget widget = new() { Size = 8, Count = 0x10, Parts = { Width = 0x20 } };
        var member = OptionsContract.GetMember(typeof(FixtureWidget), "parts", "full_width");

        member.Set(widget.Parts, 0x30);

        Assert.That(OptionsContract.GetValue(widget, "parts", "full_width"), Is.EqualTo(0x30));
    }

    [Test]
    public void Set_AMemberWithoutASetter_ThrowsSayingTheKey()
    {
        FixtureWidget widget = new() { Size = 8, Count = 0x10 };

        Assert.That(
            () => OptionsContract.GetMember(typeof(FixtureWidget), "parts").Set(widget, new FixtureParts()),
            Throws.InvalidOperationException.With.Message.EqualTo("'parts' cannot be set."));
    }

    [Test]
    public void GetCapabilities_AWidget_ListsWhatItsMembersMark()
    {
        Assert.That(OptionsContract.GetCapabilities(typeof(FixtureWidget)), Is.EqualTo(["draw", "save"]));
    }
}
