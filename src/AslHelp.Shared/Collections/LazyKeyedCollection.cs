using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AslHelp.Collections;

/// <summary>
///     Represents a keyed collection that enumerates its source lazily and caches the items it passes by key.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the items.</typeparam>
/// <remarks>
///     <para>
///         A lookup enumerates the source until it meets the key and caches every item it passes. A key that no item
///         has isn't cached, so the next lookup enumerates the source again. A cached item stays until
///         <see cref="Clear"/>, even when the source stops listing it.
///     </para>
///     <para>
///         Lookups take a lock while they enumerate the source, so they run one at a time. A source that looks items up
///         in the same collection from another thread deadlocks. When the source throws, the items cached before the
///         throw stay cached and the exception reaches the caller.
///     </para>
/// </remarks>
public abstract class LazyKeyedCollection<TKey, TValue>
    : IReadOnlyKeyedCollection<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _cache;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LazyKeyedCollection{TKey, TValue}"/> class with the default
    ///     comparer for the keys.
    /// </summary>
    protected LazyKeyedCollection()
        : this(EqualityComparer<TKey>.Default) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LazyKeyedCollection{TKey, TValue}"/> class with
    ///     <paramref name="comparer"/> for the keys.
    /// </summary>
    /// <param name="comparer">The comparer for the keys.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="comparer"/> is <see langword="null"/>.
    /// </exception>
    protected LazyKeyedCollection(IEqualityComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(comparer);

        _cache = [with(comparer)];
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Returns a copy taken under the cache's lock, so a lookup that fills the cache can't change it during
    ///     enumeration.
    /// </remarks>
    public IReadOnlyCollection<TKey> Keys
    {
        get
        {
            lock (_cache)
            {
                return [.. _cache.Keys];
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Returns a copy taken under the cache's lock, like <see cref="Keys"/>.
    /// </remarks>
    public IReadOnlyCollection<TValue> Values
    {
        get
        {
            lock (_cache)
            {
                return [.. _cache.Values];
            }
        }
    }

    /// <inheritdoc/>
    public TValue this[TKey key] => TryGetValue(key, out var value) ? value : throw new KeyNotFoundException(FormatKeyNotFound(key));

    /// <inheritdoc/>
    /// <remarks>
    ///     Enumerates the source when no item found so far has <paramref name="key"/>.
    /// </remarks>
    public bool ContainsKey(TKey key)
    {
        return TryGetValue(key, out _);
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     Enumerates the source when no item found so far has <paramref name="key"/>.
    /// </remarks>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return TryGetValue(key, this, out value);
    }

    /// <summary>
    ///     Forgets every item found so far.
    /// </summary>
    public void Clear()
    {
        lock (_cache)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    ///     Enumerates the source of the items.
    /// </summary>
    /// <returns>
    ///     The enumerator.
    /// </returns>
    public abstract IEnumerator<TValue> GetEnumerator();

    /// <summary>
    ///     Tries to get the key of <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The item.</param>
    /// <param name="key">The key of <paramref name="value"/>, when it has one.</param>
    /// <returns>
    ///     <see langword="true"/> when <paramref name="value"/> has a key and <see langword="false"/> otherwise, which
    ///     makes a lookup skip the item without caching it.
    /// </returns>
    protected abstract bool TryGetKey(TValue value, [MaybeNullWhen(false)] out TKey key);

    /// <summary>
    ///     Looks <paramref name="key"/> up among the items found so far, without enumerating the source.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The item with <paramref name="key"/>, when one found so far has it.</param>
    /// <returns>
    ///     <see langword="true"/> when an item found so far has <paramref name="key"/> and <see langword="false"/>
    ///     otherwise.
    /// </returns>
    protected bool TryGetFound(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        // Reads the cache under the lock that fills it, because a dictionary read during a write is undefined.
        lock (_cache)
        {
            return _cache.TryGetValue(key, out value);
        }
    }

    /// <summary>
    ///     Looks <paramref name="key"/> up among the items found so far, then enumerates <paramref name="source"/>
    ///     instead of the collection's own enumerator and caches every item it passes.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="source">The items to enumerate when no item found so far has <paramref name="key"/>.</param>
    /// <param name="value">The item with <paramref name="key"/>, when one has it.</param>
    /// <returns>
    ///     <see langword="true"/> when an item has <paramref name="key"/> and <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source"/> is <see langword="null"/>.
    /// </exception>
    protected bool TryGetValue(TKey key, IEnumerable<TValue> source, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (_cache)
        {
            if (_cache.TryGetValue(key, out value))
            {
                return true;
            }

            foreach (var item in source)
            {
                if (!TryGetKey(item, out var itemKey))
                {
                    continue;
                }

                // Keeps the item the source lists first when 2 items have the same key.
                _ = _cache.TryAdd(itemKey, item);

                if (_cache.Comparer.Equals(key, itemKey))
                {
                    value = item;

                    return true;
                }
            }
        }

        value = default;

        return false;
    }

    /// <summary>
    ///     Formats the message that the indexer throws for a key that no item has.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>
    ///     The message.
    /// </returns>
    protected virtual string FormatKeyNotFound(TKey key)
    {
        return $"No item has the key '{key}'.";
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
