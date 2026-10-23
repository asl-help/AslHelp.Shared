using AslHelp.Logging;
using AslHelp.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AslHelp.Tests.Options.Schema;

public partial class OptionsSchemaExporterTests
{
    [Options]
    private sealed class FixtureOptions
    {
        [Description("A plain value.")]
        public string? Kept { get; set; }

        [JsonIgnore]
        public string? Ignored { get; set; }

        [JsonPropertyName("label")]
        public string? Name { get; set; }

        [Description("A whole number.")]
        public int? Count { get; set; }

        [Description("The lowest level.")]
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
