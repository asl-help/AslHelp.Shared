using AslHelp.Shared;
using NUnit.Framework;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace AslHelp.Tests;

[TestFixture]
public class SRTests
{
    // Pins the full name because ILRepack renames a second SR with the same name and its lookup then fails.
    [Test]
    public void ResourceName_IsUniqueToThisAssembly()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(typeof(SR).FullName, Is.EqualTo("AslHelp.Shared.SR"));
            Assert.That(typeof(SR).Assembly.GetManifestResourceNames(), Does.Contain("AslHelp.Shared.SR.resources"));
        }
    }

    [Test]
    public void Property_ReadsTheResource()
    {
        Assert.That(SR.ProfileEmpty, Is.EqualTo("The profile is empty."));
    }

    [Test]
    public void FormatMethod_FillsThePlaceholders()
    {
        Assert.That(SR.FormatProfileNotFound("Widget", "1.2", 64), Is.EqualTo("No Widget profile is registered at or below Widget 1.2, 64 bit."));
    }

    [Test]
    public void Whitespace_InAValue_IsKept()
    {
        Assert.That(SR.ChoiceListLastSeparator, Is.EqualTo(" or "));
    }

    // Reads the neutral text because the assembly doesn't ship a satellite for any culture.
    [Test]
    [SetUICulture("de-DE")]
    public void OtherUICulture_FallsBackToTheNeutralText()
    {
        Assert.That(SR.ProfileEmpty, Is.EqualTo("The profile is empty."));
    }

    [Test]
    public void EveryEntry_HasText()
    {
        var set = SR.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: true);

        Assert.That(set, Is.Not.Null);

        var entries = set!.Cast<DictionaryEntry>().ToArray();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries, Is.Not.Empty);
            Assert.That(entries.Select(static entry => entry.Value as string), Has.None.Null.And.None.Empty);
        }
    }

    [Test]
    public void FailedUnwrap_ThrowsTheResourceText()
    {
        Assert.That(
            () => Result.Ok().UnwrapErr(),
            Throws.InvalidOperationException.With.Message.EqualTo("Cannot unwrap the error of Result.Ok()."));
    }
}
