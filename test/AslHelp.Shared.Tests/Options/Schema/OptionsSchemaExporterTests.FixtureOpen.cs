using AslHelp.Options;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AslHelp.Tests.Options.Schema;

public partial class OptionsSchemaExporterTests
{
    private sealed class FixtureOpen
    {
        public string? Kept { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Scoped { get; set; }
    }
}
