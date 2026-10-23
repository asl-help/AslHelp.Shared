using AslHelp.Logging;
using AslHelp.Options;
using System.ComponentModel;

[Options]
internal sealed class Settings
{
    [Description("The lowest level to log.")]
    [Choice(typeof(LogLevel))]
    public string? Level { get; set; }

    [System.Text.Json.Serialization.JsonConverter(typeof(HexNumberConverter))]
    [Description("The buffer size in bytes.")]
    public required int Size { get; set; }

    [Description("Where the log goes.")]
    public Output Output { get; set; } = new();
}

[Options]
internal sealed class Output
{
    [Description("The kind of output.")]
    [Choice(typeof(OutputKind))]
    public string Kind { get; set; } = "console";

    [Description("The file to write to.")]
    [RequiredWhen("output.kind", "file")]
    public string? Path { get; set; }
}

internal enum OutputKind
{
    Console,
    File,
}
