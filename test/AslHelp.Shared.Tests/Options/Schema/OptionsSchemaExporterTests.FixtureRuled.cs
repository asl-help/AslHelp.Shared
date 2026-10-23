using AslHelp.Options;
using System.Text.Json;

namespace AslHelp.Tests.Options.Schema;

public partial class OptionsSchemaExporterTests
{
    // Declares a rule that spans several members, the way a version entry takes either a hash or a size.
    private sealed class FixtureRuled
    {
        public string? Hash { get; set; }

        public int? Size { get; set; }

        public static void WriteRules(Utf8JsonWriter writer)
        {
            writer.WriteNumber("minProperties", 1);
        }
    }
}
