using AslHelp.Options;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AslHelp.Tests.Options.Schema;

public partial class OptionsSchemaExporterTests
{
    private sealed class FixtureVersionConverter : JsonConverter<Version>
    {
        public static void WriteSchema(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "string");
            writer.WriteString("pattern", "^[0-9]+");
            writer.WriteEndObject();
        }

        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new(reader.GetString() ?? "");
        }

        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
