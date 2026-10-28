using AslHelp.Options.Binding;
using System;
using System.Collections.Generic;

namespace AslHelp.Options;

/// <summary>
///     Marks an option as required where another option of the same document has one of the given values, such as
///     an offset only one family of a runtime reads.
/// </summary>
/// <remarks>
///     <see cref="OptionsValidator.ThrowIfMissingRequired(object, string)"/> reads the mark beside the C#
///     <see langword="required"/> one, so an option shared by several families is required of each family that reads
///     it and of no other.
/// </remarks>
/// <param name="option">The deciding option's keys from the document's root, joined by dots, such as <c>family</c>.</param>
/// <param name="values">The values of the deciding option that make this one required, compared without case.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class RequiredWhenAttribute(
    string option,
    params string[] values) : Attribute
{
    /// <summary>
    ///     Gets the deciding option's keys from the document's root, joined by dots.
    /// </summary>
    public string Option { get; } = option;

    /// <summary>
    ///     Gets the values of the deciding option that make this one required.
    /// </summary>
    public IReadOnlyList<string> Values { get; } = values;
}
