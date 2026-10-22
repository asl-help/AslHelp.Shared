using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AslHelp.Collections;

/// <summary>
///     Represents a read-only sequence whose items are found by the key of each item.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TValue">The type of the items.</typeparam>
public interface IReadOnlyKeyedCollection<TKey, TValue>
    : IEnumerable<TValue>
    where TKey : notnull
{
    /// <summary>
    ///     Gets the item with <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <value>
    ///     The item with <paramref name="key"/>.
    /// </value>
    /// <exception cref="KeyNotFoundException">
    ///     No item has <paramref name="key"/>.
    /// </exception>
    TValue this[TKey key] { get; }

    /// <summary>
    ///     Gets the keys of the items.
    /// </summary>
    IReadOnlyCollection<TKey> Keys { get; }

    /// <summary>
    ///     Gets the items.
    /// </summary>
    IReadOnlyCollection<TValue> Values { get; }

    /// <summary>
    ///     Determines whether an item has <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>
    ///     <see langword="true"/> when an item has <paramref name="key"/> and <see langword="false"/> otherwise.
    /// </returns>
    bool ContainsKey(TKey key);

    /// <summary>
    ///     Gets the item with <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The item with <paramref name="key"/>, when one has it.</param>
    /// <returns>
    ///     <see langword="true"/> when an item has <paramref name="key"/> and <see langword="false"/> otherwise.
    /// </returns>
    bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
}
