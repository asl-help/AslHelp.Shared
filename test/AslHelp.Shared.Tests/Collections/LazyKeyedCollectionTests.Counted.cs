using AslHelp.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AslHelp.Tests.Collections;

public partial class LazyKeyedCollectionTests
{
    private sealed class Counted(
        string?[] items) : LazyKeyedCollection<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        public int Enumerations { get; private set; }

        public int? ThrowAt { get; init; }

        public override IEnumerator<string> GetEnumerator()
        {
            Enumerations++;

            for (var i = 0; i < items.Length; i++)
            {
                if (i == ThrowAt)
                {
                    throw new InvalidOperationException($"item {i} is unreadable");
                }

                yield return items[i] ?? "";
            }
        }

        public bool TryGetValueIn(string key, IEnumerable<string> source, out string? value)
        {
            return TryGetValue(key, source, out value);
        }

        public bool HasFound(string key)
        {
            return TryGetFound(key, out _);
        }

        protected override bool TryGetKey(string value, [MaybeNullWhen(false)] out string key)
        {
            key = value;

            return value.Length != 0;
        }

        protected override string FormatKeyNotFound(string key)
        {
            return $"counted has no '{key}'.";
        }
    }

    private sealed class Compared(
        IEqualityComparer<string> comparer) : LazyKeyedCollection<string, string>(comparer)
    {
        public override IEnumerator<string> GetEnumerator()
        {
            yield break;
        }

        protected override bool TryGetKey(string value, out string key)
        {
            key = value;

            return true;
        }
    }

    private sealed class Numbered(
        int[] items) : LazyKeyedCollection<int, int>
    {
        public override IEnumerator<int> GetEnumerator()
        {
            return ((IEnumerable<int>)items).GetEnumerator();
        }

        protected override bool TryGetKey(int value, out int key)
        {
            key = value;

            return value >= 0;
        }
    }
}
