using AslHelp.Options.Profiles;
using NUnit.Framework;
using System.Text.Json;

namespace AslHelp.Tests.Options.Profiles;

[TestFixture]
public class ProfileVersionsTests
{
    private static readonly string[] _lines = ["2017.2", "2021.3", "2018.4"];

    [Test]
    public void NewestAtOrBelow_ATargetBetweenTwoEntries_TakesTheLowerOne()
    {
        bool found = ProfileVersions.TryNewestAtOrBelow<string>(_lines, static line => line, "2020.1.18f1", out string? chosen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(chosen, Is.EqualTo("2018.4"));
        }
    }

    // A version read from a target's files can have any number of digits, which must order instead of overflowing.
    [Test]
    public void Components_ADigitRunTooLongForAnInt_ReadsAsTheLargestInt()
    {
        Assert.That(ProfileVersions.Components("2022.3.12345678901f1")[2], Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void NewestAtOrBelow_ATargetOnAnEntry_TakesThatEntry()
    {
        bool found = ProfileVersions.TryNewestAtOrBelow<string>(_lines, static line => line, "2021.3.11f1", out string? chosen);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Is.True);
            Assert.That(chosen, Is.EqualTo("2021.3"));
        }
    }

    [Test]
    public void NewestAtOrBelow_ATargetBelowEveryEntry_ChoosesNothing()
    {
        bool found = ProfileVersions.TryNewestAtOrBelow<string>(_lines, static line => line, "5.6.7f1", out _);

        Assert.That(found, Is.False);
    }

    // A layout can change between 2 alphas of 1 patch, so the increment takes part in the choice.
    [Test]
    public void NewestAtOrBelow_TwoAlphasOfOnePatch_TakesTheOneAtOrBelow()
    {
        string[] entries = ["6000.5.0f1", "6000.7.0a2", "6000.7.0a5"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Choose(entries, "6000.7.0a1"), Is.EqualTo("6000.5.0f1"));
            Assert.That(Choose(entries, "6000.7.0a4"), Is.EqualTo("6000.7.0a2"));
            Assert.That(Choose(entries, "6000.7.0a6"), Is.EqualTo("6000.7.0a5"));
        }
    }

    [Test]
    public void NewestAtOrBelow_TwoFinalsOfOnePatch_TakesTheOneAtOrBelow()
    {
        string[] entries = ["5.0.0f4", "5.3.0f4"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Choose(entries, "5.3.0f2"), Is.EqualTo("5.0.0f4"));
            Assert.That(Choose(entries, "5.3.0f4"), Is.EqualTo("5.3.0f4"));
        }
    }

    // An entry that gives only a release line applies to every release in that line, alphas included.
    [Test]
    public void NewestAtOrBelow_AnEntryNamingALine_AppliesToEveryReleaseOfIt()
    {
        string[] entries = ["2021.3", "2022.1"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Choose(entries, "2022.1.0a1"), Is.EqualTo("2022.1"));
            Assert.That(Choose(entries, "2021.3.45f1"), Is.EqualTo("2021.3"));
        }
    }

    // A target that gives only part of a version stands for the whole line, as an entry that gives a line does, so the
    // newest entry of that line matches rather than an entry from the line below.
    [Test]
    public void NewestAtOrBelow_ATargetLeavingPartsOff_TakesTheNewestEntryOfItsLine()
    {
        string[] entries = ["2017.3.0f3", "2017.4.0f1", "2017.4.30f1", "2018.1.0f1", "2018.2.0f1"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Choose(entries, "2017.4"), Is.EqualTo("2017.4.30f1"));
            Assert.That(Choose(entries, "2018.1.0"), Is.EqualTo("2018.1.0f1"));
            Assert.That(Choose(entries, "2018.1.0f1"), Is.EqualTo("2018.1.0f1"));
        }
    }

    [TestCase("2022.3.5f1")]
    [TestCase("4.24")]
    [TestCase("3.8225")]
    [TestCase("2019.3.0f11-Sunflower")]
    [TestCase(null)]
    public void Check_AnEngineVersionOrNone_PassesIt(string? version)
    {
        Assert.That(() => ProfileVersions.Check(version, "The options file 'options.json'", "version"), Throws.Nothing);
    }

    [TestCase("abc")]
    [TestCase("2022")]
    [TestCase("v4.24")]
    public void Check_TextThatIsNoEngineVersion_ThrowsSayingTheOptionAndTheValue(string version)
    {
        Assert.That(
            () => ProfileVersions.Check(version, "The options file 'options.json'", "version"),
            Throws.TypeOf<JsonException>().With.Message.EqualTo(
                $"The options file 'options.json' declares the 'version' value '{version}', which is no engine version. "
                    + "Write it the way the engine spells it, such as \"2022.3.5f1\", \"4.24\" or \"3.8225\"."));
    }

    [Test]
    public void Compare_ReleaseTypes_OrderAsUnityOrdersThem()
    {
        string[] ordered = ["2019.3.0a9", "2019.3.0b1", "2019.3.0f1", "2019.3.0p1", "2019.3.0x"];

        for (int i = 1; i < ordered.Length; i++)
        {
            Assert.That(
                ProfileVersions.Compare(ProfileVersions.Components(ordered[i - 1]), ProfileVersions.Components(ordered[i])),
                Is.Negative,
                $"{ordered[i - 1]} before {ordered[i]}");
        }
    }

    // A part one version leaves off reads as 0 on either side, so swapping the 2 versions flips the sign. A sort
    // is stable in any order of comparison.
    [Test]
    public void Compare_AVersionWithAPartTheOtherLeavesOff_FlipsTheSignWhenSwapped()
    {
        int[] shorter = [2021, 3];
        int[] longer = [2021, 3, 5];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ProfileVersions.Compare(shorter, longer), Is.Negative);
            Assert.That(ProfileVersions.Compare(longer, shorter), Is.Positive);
        }
    }

    [TestCase("2022.3.8f1", "2022.3.8c1")]
    [TestCase("2019.3.0f11", "2019.3.0f11-Sunflower")]
    [TestCase("2022.3.8f1", "2022.3.8f1c1")]
    [TestCase("2020.1.0", "2020.1.0q7")]
    public void Compare_WhatUnityIgnores_ComparesEqual(string version, string same)
    {
        Assert.That(ProfileVersions.Compare(ProfileVersions.Components(version), ProfileVersions.Components(same)), Is.Zero);
    }

    private static string? Choose(string[] entries, string target)
    {
        _ = ProfileVersions.TryNewestAtOrBelow<string>(entries, static entry => entry, target, out string? chosen);

        return chosen;
    }
}
