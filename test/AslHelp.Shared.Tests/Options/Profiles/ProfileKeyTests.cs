using AslHelp.Options.Profiles;
using NUnit.Framework;

namespace AslHelp.Tests.Options.Profiles;

[TestFixture]
public class ProfileKeyTests
{
    // Keys equal in any case hash the same because a catalogue finds an entry under another spelling.
    [Test]
    public void GetHashCode_TwoSpellingsOfOneKey_Agree()
    {
        Assert.That(new ProfileKey("alpha", "2021.3.0F1", 8).GetHashCode(), Is.EqualTo(new ProfileKey("ALPHA", "2021.3.0f1", 8).GetHashCode()));
    }

    [Test]
    public void GetHashCode_ADefaultKey_HashesLikeAnEmptyOne()
    {
        Assert.That(default(ProfileKey).GetHashCode(), Is.EqualTo(new ProfileKey("", "", 0).GetHashCode()));
    }

    [TestCase("alpha-2021.3-x64", true, "alpha", "2021.3", 8)]
    [TestCase("29-6000.5-x86", true, "29", "6000.5", 4)]
    [TestCase("5.8-x64", false, "", "5.8", 8)]
    public void TryParseShippedName_AWellFormedName_ReadsTheKey(string name, bool partitioned, string partition, string version, int pointerSize)
    {
        bool parsed = ProfileKey.TryParseShippedName(name, partitioned, out var key);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed, Is.True);
            Assert.That(key, Is.EqualTo(new ProfileKey(partition, version, pointerSize)));
        }
    }

    [TestCase("5.8-x64", true)]
    [TestCase("5.8", false)]
    [TestCase("alpha-2021.3-arm", true)]
    [TestCase("-x64", false)]
    public void TryParseShippedName_ANameWithoutTheShape_Refuses(string name, bool partitioned)
    {
        Assert.That(ProfileKey.TryParseShippedName(name, partitioned, out _), Is.False);
    }
}
