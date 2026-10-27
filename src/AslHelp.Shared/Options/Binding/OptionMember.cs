using AslHelp.Options;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;

namespace AslHelp.Options.Binding;

/// <summary>
///     Describes 1 member the serializer binds on an options type: the key the document spells it with, its type
///     and the marks the options type declares on it.
/// </summary>
/// <remarks>
///     The key is the serializer's own, so a member renamed with <c>[JsonPropertyName]</c> reads under that name and
///     every other under its camel-cased name, exactly as a document binds and a schema spells it.
/// </remarks>
public sealed class OptionMember
{
    internal OptionMember(
        JsonPropertyInfo property,
        bool isRequired,
        IReadOnlyList<string> capabilities,
        bool isSection,
        RequiredWhenAttribute? requiredWhen,
        OptionValidationAttribute? validator)
    {
        Property = property;
        IsRequired = isRequired;
        Capabilities = capabilities;
        IsSection = isSection;
        RequiredWhen = requiredWhen;
        Validator = validator;
    }

    /// <summary>
    ///     Gets the key the document spells the member with.
    /// </summary>
    public string Key => Property.Name;

    /// <summary>
    ///     Gets the member's type.
    /// </summary>
    public Type Type => Property.PropertyType;

    /// <summary>
    ///     Gets a value indicating whether the member is a section (a type marked <c>[Options]</c>) rather than a leaf.
    /// </summary>
    public bool IsSection { get; }

    /// <summary>
    ///     Gets a value indicating whether the member is declared <see langword="required"/>.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    ///     Gets the capabilities the member is marked with, in the order the marks are declared.
    /// </summary>
    public IReadOnlyList<string> Capabilities { get; }

    /// <summary>
    ///     Gets the option and values that make the member required. <see langword="null"/> when no such mark is
    ///     declared.
    /// </summary>
    public RequiredWhenAttribute? RequiredWhen { get; }

    internal JsonPropertyInfo Property { get; }

    internal OptionValidationAttribute? Validator { get; }

    internal bool CanGet => Property.Get is not null;

    internal bool CanSet => Property.Set is not null;

    /// <summary>
    ///     Reads the member off an options object.
    /// </summary>
    /// <param name="options">The object, of the type the member was listed from.</param>
    /// <returns>
    ///     The value. <see langword="null"/> when the member has no value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The member has no getter.
    /// </exception>
    public object? Get(object options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (Property.Get is not { } get)
        {
            throw new InvalidOperationException($"'{Key}' cannot be read.");
        }

        return get(options);
    }

    /// <summary>
    ///     Writes the member on an options object.
    /// </summary>
    /// <param name="options">The object, of the type the member was listed from.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The member has no setter.
    /// </exception>
    public void Set(object options, object? value)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (Property.Set is not { } set)
        {
            throw new InvalidOperationException($"'{Key}' cannot be set.");
        }

        set(options, value);
    }
}
