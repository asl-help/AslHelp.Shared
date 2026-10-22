using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace AslHelp.Tests.Collections;

[TestFixture]
public partial class LazyKeyedCollectionTests
{
    // The indexer returns the item with the key.
    [Test]
    public void Indexer_AKeyTheSourceHas_ReturnsTheItem()
    {
        Counted counted = new(["one", "two", "three"]);

        Assert.That(counted["two"], Is.EqualTo("two"));
    }

    // A lookup enumerates only until it meets the key and caches every item it passed.
    [Test]
    public void Indexer_AKeyTheSourceHas_CachesTheItemsUpToIt()
    {
        Counted counted = new(["one", "two", "three"]);

        _ = counted["two"];

        Assert.That(counted.Keys, Is.EqualTo(["one", "two"]));
    }

    // Values lists the cached items in the order the source lists them.
    [Test]
    public void Values_AfterALookup_ListsTheCachedItems()
    {
        Counted counted = new(["one", "two", "three"]);

        _ = counted["two"];

        Assert.That(counted.Values, Is.EqualTo(["one", "two"]));
    }

    // A key found once comes from the cache without enumerating the source again.
    [Test]
    public void TryGetValue_AKeyAlreadyFound_DoesNotEnumerateAgain()
    {
        Counted counted = new(["one", "two"]);
        _ = counted["two"];

        _ = counted.TryGetValue("one", out _);

        Assert.That(counted.Enumerations, Is.EqualTo(1));
    }

    // A miss isn't cached, since a key missing now may be added to the source later.
    [Test]
    public void TryGetValue_AKeyTheSourceLacks_EnumeratesAgainNextTime()
    {
        Counted counted = new(["one"]);

        _ = counted.TryGetValue("two", out _);
        _ = counted.TryGetValue("two", out _);

        Assert.That(counted.Enumerations, Is.EqualTo(2));
    }

    // A duplicate key returns the item the source lists first.
    [Test]
    public void Indexer_ADuplicateKey_ReturnsTheFirstItem()
    {
        Counted counted = new(["One", "ONE"]);

        Assert.That(counted["one"], Is.EqualTo("One"));
    }

    // An item without a key is skipped and not cached.
    [Test]
    public void TryGetValue_AnItemWithoutAKey_SkipsIt()
    {
        Counted counted = new([null, "two"]);

        _ = counted["two"];

        Assert.That(counted.Keys, Is.EqualTo(["two"]));
    }

    // A source that throws keeps the items cached before the throw and passes the exception on.
    [Test]
    public void TryGetValue_ASourceThatThrows_KeepsTheItemsBefore()
    {
        Counted counted = new(["one", "two", "three"]) { ThrowAt = 2 };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => counted.TryGetValue("three", out _),
                Throws.InvalidOperationException.With.Message.EqualTo("item 2 is unreadable"));
            Assert.That(counted.Keys, Is.EqualTo(["one", "two"]));
        }
    }

    // A derived collection can enumerate another source than its own and still fill the cache.
    [Test]
    public void TryGetValue_OverAGivenSource_CachesWhatItPasses()
    {
        Counted counted = new(["one"]);

        _ = counted.TryGetValueIn("three", ["two", "three", "four"], out _);

        Assert.That(counted.Keys, Is.EqualTo(["two", "three"]));
    }

    // A lookup over a given source leaves the collection's own source alone.
    [Test]
    public void TryGetValue_OverAGivenSource_DoesNotEnumerateItsOwn()
    {
        Counted counted = new(["one"]);

        _ = counted.TryGetValueIn("three", ["three"], out _);

        Assert.That(counted.Enumerations, Is.Zero);
    }

    // The protected lookup refuses a null source.
    [Test]
    public void TryGetValue_WithNullSource_Throws()
    {
        Counted counted = new(["one"]);

        Assert.That(
            () => counted.TryGetValueIn("one", null!, out _),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("source"));
    }

    // TryGetFound misses a key that no lookup has met and doesn't enumerate the source.
    [Test]
    public void TryGetFound_AKeyNoLookupHasMet_IsMissingAndEnumeratesNothing()
    {
        Counted counted = new(["one"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(counted.HasFound("one"), Is.False);
            Assert.That(counted.Enumerations, Is.Zero);
        }
    }

    // TryGetFound finds a key that a lookup has met.
    [Test]
    public void TryGetFound_AKeyALookupHasMet_IsFound()
    {
        Counted counted = new(["one"]);
        _ = counted["one"];

        Assert.That(counted.HasFound("one"), Is.True);
    }

    // ContainsKey finds a key the source lists.
    [Test]
    public void ContainsKey_ForEitherKey_FindsOnlyListedKeys()
    {
        Counted counted = new(["one"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(counted.ContainsKey("one"), Is.True);
            Assert.That(counted.ContainsKey("two"), Is.False);
        }
    }

    // Clear forgets the cached items.
    [Test]
    public void Clear_AfterALookup_EmptiesTheCache()
    {
        Counted counted = new(["one"]);
        _ = counted["one"];

        counted.Clear();

        Assert.That(counted.Keys, Is.Empty);
    }

    // The next lookup after Clear enumerates the source again.
    [Test]
    public void Clear_AfterALookup_MakesTheNextLookupEnumerateAgain()
    {
        Counted counted = new(["one"]);
        _ = counted["one"];
        counted.Clear();

        _ = counted["one"];

        Assert.That(counted.Enumerations, Is.EqualTo(2));
    }

    // The indexer throws with the message the derived collection formats, with the key in it.
    [Test]
    public void Indexer_AKeyTheSourceLacks_ThrowsWithTheDerivedMessage()
    {
        Counted counted = new([]);

        Assert.That(
            () => counted["missing"],
            Throws.InstanceOf<KeyNotFoundException>().With.Message.EqualTo("counted has no 'missing'."));
    }

    // Keys are compared with the comparer the derived collection passes.
    [Test]
    public void Indexer_ACaseInsensitiveComparer_MatchesAnyCase()
    {
        Counted counted = new(["One"]);

        Assert.That(counted["ONE"], Is.EqualTo("One"));
    }

    // The constructor refuses a null comparer.
    [Test]
    public void Constructor_WithNullComparer_Throws()
    {
        Assert.That(
            () => new Compared(null!),
            Throws.ArgumentNullException.With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("comparer"));
    }

    // Enumerating the collection yields every item and caches none of them.
    [Test]
    public void GetEnumerator_OverTheCollection_CachesNothing()
    {
        Counted counted = new(["one", "two"]);

        Assert.That(counted, Is.EqualTo(["one", "two"]));
        Assert.That(counted.Keys, Is.Empty);
    }

    // A value-type key that's the default value is found like any other key.
    [Test]
    public void TryGetValue_AnIntKeyOfZero_FindsTheItem()
    {
        Numbered numbered = new([-1, 0, 1]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(numbered.TryGetValue(0, out var value), Is.True);
            Assert.That(value, Is.Zero);
            Assert.That(numbered.Keys, Is.EqualTo([0]));
        }
    }
}
