using AslHelp.Logging;
using AslHelp.Options;

[Options]
internal sealed class Settings
{
    [Choice(typeof(LogLevel))]
    public string? Level { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(HexNumberConverter))]
    public required int Size { get; set; }

    public Output Output { get; set; } = new();
}

[Options]
internal sealed class Output
{
    [Choice(typeof(OutputKind))]
    public string Kind { get; set; } = "console";

    [RequiredWhen("output.kind", "file")]
    public string? Path { get; set; }
}

internal enum OutputKind
{
    Console,
    File,
}
