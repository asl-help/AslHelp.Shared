using AslHelp.Options;
using System.Collections.Generic;

namespace AslHelp.Tests.Options.Schema;

public partial class OptionsSchemaExporterTests
{
    private sealed class FixtureNode
    {
        public Dictionary<string, FixtureNode>? Children { get; set; }
    }
}
