using System;
using System.Linq;
using System.Text.Json;

namespace AslHelp.Options;

/// <summary>
///     Marks a string option as a choice. Its value is one of an enum's members, spelled the way
///     <see cref="Choice.Parse{TEnum}(string, string, string)"/> reads them. The generated schema lists those spellings.
/// </summary>
/// <param name="enumType">The enum whose members are the accepted choices.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ChoiceAttribute(
    Type enumType) : OptionCheckAttribute
{
    /// <summary>
    ///     Gets the enum whose members are the accepted choices.
    /// </summary>
    public Type EnumType { get; } = enumType;

    /// <inheritdoc/>
    public override void Check(object value, string origin, string path)
    {
        ArgumentNullException.ThrowIfNull(value);

        _ = Choice.Parse(EnumType, value.ToString(), origin, path);
    }

    /// <inheritdoc/>
    public override void WriteSchema(Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStartObject();
        writer.WriteString("type", "string");
        writer.WriteStartArray("enum");

        foreach (string spelling in Choice.Spellings(EnumType).Concat(Choice.Aliases(EnumType)))
        {
            writer.WriteStringValue(spelling);
        }

        writer.WriteEndArray();
        writer.WriteString("description", "A spelling matches ignoring case.");
        writer.WriteEndObject();
    }
}
