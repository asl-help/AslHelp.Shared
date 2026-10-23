using AslHelp.Logging;
using AslHelp.Tests.Logging;
using NUnit.Framework;
using System;
using System.Linq;

namespace AslHelp.Tests.Options.Profiles;

// Uses a catalogue defined in this test so the shared behavior of every versioned catalogue is tested apart from any
// real entries.
[TestFixture]
public partial class VersionedProfileCatalogueTests
{
    [Test]
    public void For_AVersionBetweenEntries_TakesTheNewestAtOrBelowItInItsPartitionAndWidth()
    {
        FixtureCatalogue catalogue = new();
        catalogue.Register("alpha", "2018.1", 8, """{ "size": 1 }""");
        catalogue.Register("alpha", "2019.4", 8, """{ "size": 2 }""");
        catalogue.Register("alpha", "2019.4", 4, """{ "size": 3 }""");
        catalogue.Register("beta", "2019.4", 8, """{ "size": 4 }""");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(catalogue.For("alpha", "2019.2.0f1", 8).Unwrap().Size, Is.EqualTo(1));
            Assert.That(catalogue.For("Alpha", "2021.3.0f1", 8).Unwrap().Size, Is.EqualTo(2));
            Assert.That(catalogue.For("alpha", "2021.3.0f1", 4).Unwrap().Size, Is.EqualTo(3));
            Assert.That(catalogue.For("beta", "2021.3.0f1", 8).Unwrap().Size, Is.EqualTo(4));
        }
    }

    [Test]
    public void For_AVersionBelowEveryEntry_FailsSayingTheEngineTheTargetAndTheWidth()
    {
        FixtureCatalogue catalogue = new();
        catalogue.Register("alpha", "2019.4", 8, """{ "size": 2 }""");

        Assert.That(
            catalogue.For("alpha", "2017.4.0f1", 8).Error!.Message,
            Is.EqualTo("No Fixture profile is registered at or below Fixture alpha 2017.4.0f1, 64 bit."));
    }

    [Test]
    public void Register_TheSameKeyInAnotherCase_ReplacesTheEntry()
    {
        FixtureCatalogue catalogue = new();
        catalogue.Register("alpha", "2019.4f1", 8, """{ "size": 2 }""");
        catalogue.Register("ALPHA", "2019.4F1", 8, """{ "size": 5 }""");

        Assert.That(catalogue.For("alpha", "2019.4f1", 8).Unwrap().Size, Is.EqualTo(5));
    }

    [Test]
    public void Register_AWidthThatIsNeither4Nor8_Throws()
    {
        FixtureCatalogue catalogue = new();

        Assert.That(
            () => catalogue.Register("alpha", "2019.4", 2, """{ "size": 2 }"""),
            Throws.TypeOf<ArgumentOutOfRangeException>().With.Message.Contains("4 or 8"));
    }

    // The log line and any error about an entry refer to the entry by its file name.
    [Test]
    public void For_AnEntryThatApplies_WritesItByTheNameItsFileHas()
    {
        FixtureCatalogue catalogue = new();
        catalogue.Register("alpha", "2019.4", 8, """{ "size": 2 }""");
        RecordingSink sink = new(LogLevel.Debug);
        using Logger log = new();
        log.Add(sink);

        _ = catalogue.For("alpha", "2020.1.0f1", 8, log: log, kind: "foo");

        Assert.That(sink.Lines.Select(static line => line.Event.Message), Has.Some.EqualTo("fixture entry alpha-2019.4-x64, foo scoped"));
    }

    // The catalogue plants values onto a fresh parse so an override planted after them wins.
    [Test]
    public void For_WithSomethingToPlant_PlantsOnAFreshParseAndLetsAnOverrideWin()
    {
        FixtureCatalogue catalogue = new();
        catalogue.Register("alpha", "2019.4", 8, """{ "size": 2 }""");

        var scoped = catalogue.For("alpha", "2019.4", 8, kind: "foo").Unwrap();
        var overridden = catalogue.For("alpha", "2019.4", 8, new FixtureProfile { Size = null, Kind = "bar" }, kind: "foo").Unwrap();
        var plain = catalogue.For("alpha", "2019.4", 8).Unwrap();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scoped.Kind, Is.EqualTo("foo"));
            Assert.That(overridden.Kind, Is.EqualTo("bar"));
            Assert.That(plain.Kind, Is.Null);
        }
    }

    [Test]
    public void For_SomethingToPlantOnAnEntryRegisteredAsAProfile_FailsSayingToRegisterTheDocument()
    {
        FixtureCatalogue catalogue = new();
        catalogue.Register("alpha", "2019.4", 8, new FixtureProfile { Size = 2 });

        Assert.That(
            catalogue.For("alpha", "2019.4", 8, kind: "foo").Error!.Message,
            Is.EqualTo(
                "The entry for alpha-2019.4-x64 was registered as a parsed profile rather than from its document, "
                    + "so there is nothing to plant values onto. Register the document to override or scope it."));
    }
}
