using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AslHelp.Options;

/// <summary>
///     Binds a whole number written as a JSON number or as a <c>"0x"</c> hex string.
/// </summary>
public sealed class HexNumberConverter : JsonConverter<int>
{
    /// <inheritdoc/>
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        // Leaves the value's path out of the error because the converter serves whole documents and doesn't get the
        // path. The code that reads a user's file appends the path.
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("A whole number is a json number or a '0x' hex string.");
        }

        return HexNumber.Parse(reader.GetString()!, "A whole number");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);

        writer.WriteStringValue($"0x{value:X}");
    }
}
