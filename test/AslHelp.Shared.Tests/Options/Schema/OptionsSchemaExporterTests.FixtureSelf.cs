using AslHelp.Options;
using System.Text.Json;

namespace AslHelp.Tests.Options.Schema;

public partial class OptionsSchemaExporterTests
{
    // Writes its own schema, the way a custom value type does.
    private sealed class FixtureSelf
    {
        public static void WriteSchema(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("type", "array");
            writer.WriteEndObject();
        }
    }
}
