using AslHelp.Logging;
using AslHelp.Options;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AslHelp.Tests.Options.Schema;

public partial class OptionsSchemaExporterTests
{
    [Options]
    private sealed class FixtureOptions
    {
        public string? Kept { get; set; }

        [JsonIgnore]
        public string? Ignored { get; set; }

        [JsonPropertyName("label")]
        public string? Name { get; set; }

        public int? Count { get; set; }

        [Choice(typeof(LogLevel))]
        public string? Level { get; set; }

        [JsonConverter(typeof(FixtureVersionConverter))]
        public Version? Version { get; set; }

        public FixtureSelf? Self { get; set; }

        public FixtureRuled? Ruled { get; set; }

        public Dictionary<string, FixtureNode>? Nodes { get; set; }

        public List<int>? Sizes { get; set; }

        public Dictionary<string, int>? Fields { get; set; }

        public FixtureOpen? Open { get; set; }
    }
}
