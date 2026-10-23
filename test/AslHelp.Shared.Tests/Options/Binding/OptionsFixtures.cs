using AslHelp.Logging;
using AslHelp.Options;
using AslHelp.Options.Capabilities;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AslHelp.Tests.Options.Binding;

internal static class OptionsFixtures
{
    public static FixtureWidget Full()
    {
        return new() { Kind = "beta", Size = 0x20, Count = 0x0, Parts = { Width = 0x4C0, Name = 0x48, Depth = 0x100 } };
    }
}

internal enum FixtureColor
{
    Red,
    Green,
}

[Options]
internal class FixtureBase
{
    public string? Title { get; set; }
}

internal sealed class FixtureLeaf
{
    public int? Anything { get; set; }
}

// `SetsRequiredMembers` waives the required check so a sparse override can exist. `Require` restores the check.
[Options]
[method: SetsRequiredMembers]
internal sealed class FixtureParts()
{
    [JsonPropertyName("full_width")]
    public required int? Width { get; set; }

    public int? Name { get; set; }

    // Requires this value for the `beta` kind only, because no other kind reads it.
    [JsonPropertyName("full_depth")]
    [RequiredWhen("kind", "beta")]
    public int? Depth { get; set; }
}

[Options]
internal sealed class FixtureGadget
{
    [Choice(typeof(FixtureColor))]
    public string? Color { get; set; }
}

[Options]
internal sealed class FixtureWidget
{
    public string? Kind { get; set; }

    public required int? Size { get; set; }

    // Leaves this value unset only when `draw` is unavailable together with `save`, because both capabilities read it.
    [Capability("draw")]
    [Capability("save")]
    public required int? Count { get; set; }

    [Capability("draw")]
    public FixtureParts Parts { get; } = new();
}

// Declares 1 member of each kind the serializer decides about: a renamed key, an ignored member, an opted-in
// field, a field left out, an inherited member, a section and a leaf object that is not a section.
[Options]
internal sealed class FixtureMembers : FixtureBase
{
    [JsonPropertyName("label")]
    public string? Name { get; set; }

    [JsonIgnore]
    public string? Ignored { get; set; }

    [JsonInclude]
    public int? Opted;

    public int? Bare;

    public FixtureParts Parts { get; } = new();

    public FixtureLeaf? Leaf { get; set; }

    public List<FixtureLeaf>? Leaves { get; set; }

    public Dictionary<string, FixtureLeaf>? Named { get; set; }
}

[Options]
internal sealed class FixtureOpen
{
    public string? Kept { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Scoped { get; set; }
}

// Declares one checked option at the root and one inside a section.
[Options]
internal sealed class FixtureChecked
{
    [Choice(typeof(LogLevel))]
    public string? Level { get; set; }

    public FixtureGadget Gadget { get; } = new();
}
