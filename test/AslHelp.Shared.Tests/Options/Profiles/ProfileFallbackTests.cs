using AslHelp.Options.Profiles;
using NUnit.Framework;

namespace AslHelp.Tests.Options.Profiles;

// An interval starts at its first version and stops before its end version, ordered the way Unity orders releases.
[TestFixture]
public class ProfileFallbackTests
{
    private static readonly ProfileFallback _early53 = new("5.3.0f1", "5.3.0f4", "5.3.0f4");

    [TestCase("5.3.0f1", true)]
    [TestCase("5.3.0f2", true)]
    [TestCase("5.3.0f3", true)]
    [TestCase("5.3.0f4", false)]
    [TestCase("5.2.5f1", false)]
    [TestCase("5.3.1f1", false)]
    public void Covers_APatch_IsInsideFromItsStartUntilBeforeItsEnd(string version, bool covered)
    {
        Assert.That(_early53.Covers(version), Is.EqualTo(covered));
    }

    // An alpha comes before a final release of the same version, so a6 lies inside an interval that ends at the final.
    [Test]
    public void Covers_AnAlphaBeforeTheEnd_IsInside()
    {
        ProfileFallback alphas = new("6000.7.0a2", "6000.7.0f1", "6000.5.0f1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(alphas.Covers("6000.7.0a5"), Is.True);
            Assert.That(alphas.Covers("6000.7.0a1"), Is.False);
            Assert.That(alphas.Covers("6000.7.0f1"), Is.False);
        }
    }

    [Test]
    public void Covers_WithNoVersion_ThrowsSayingTheArgument()
    {
        Assert.That(() => _early53.Covers(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("version"));
    }
}
