# AslHelp.Shared

[![AslHelp.Shared on NuGet](https://img.shields.io/nuget/vpre/AslHelp.Shared?label=AslHelp.Shared)](https://www.nuget.org/packages/AslHelp.Shared)

AslHelp.Shared is a small .NET library with the pieces the other AslHelp packages share. It has `Result` types for operations that fail without throwing, a `Logger` with sinks, a lazy keyed collection and options that bind from JSONC with checks and a schema.

## Install

```
dotnet add package AslHelp.Shared
```

## Usage

### Returning a result

A method returns a `Result<T>` with either a value or a `ResultError`. Each kind of failure gets its own error record:

```cs
public sealed record LevelError(string Message) : ResultError
{
    public override string Message { get; } = Message;
}

Result<int> ReadLevel(int raw)
{
    if (raw < 0)
    {
        return new LevelError($"Level {raw} is negative.");
    }

    return raw;
}
```

`Result` works the same way for operations that don't return a value.

### Reading a result

`TryUnwrap` reads a result and hands the error back up when it failed:

```cs
Result<string> DescribeLevel(int raw)
{
    if (!ReadLevel(raw).TryUnwrap(out var level, out var error))
    {
        return error;
    }

    return $"Level {level}";
}
```

`Unwrap`, `UnwrapOr` and the other `Unwrap` methods read it in other ways.

### Chaining

`AndThen` and `Map` run the next step only after a success. `Match` turns the outcome into one value:

```cs
string DescribeNextLevel(int raw)
{
    return ReadLevel(raw)
        .AndThen(level => ReadLevel(level + 1))
        .Map(level => level * 2)
        .Match(level => $"Level {level}", error => error.Message);
}
```

### Exceptions

`ExceptionError` turns a caught exception into a failure. It keeps the exception and uses its message unless you pass your own:

```cs
try
{
    return File.ReadAllText(path);
}
catch (IOException ex)
{
    return new ExceptionError(ex, $"Couldn't read {path}.");
}
```

### Logging

A `Logger` writes each line to every sink that accepts its level. `FileSink` writes to a file on its own thread and trims the file when it grows past `maxLines`, so a long run never fills the disk:

```cs
using var logger = new Logger();
logger.AddSink(new FileSink("logs/run.log", LogLevel.Debug, maxLines: 10_000, linesToErase: 1_000));

var script = logger.WithPrefix("Celeste");

script.LogInformation($"Found level {level}.");

using (script.BeginScope(LogLevel.Debug, "Reading the player"))
{
    script.LogDebug($"Health is {health}.");
}
```

The `LogTrace` through `LogError` methods only build their message when a sink writes at that level, so a trace line in a hot loop costs nothing while trace is off. `WithPrefix` returns a view over the same sinks that writes under another name. `BeginScope` indents every line until it is disposed.

A line looks like `[2026-10-28 12:00:00.000] [info] [Celeste] Found level 3.`. `LogFormatter.Create(minimumLevel, includeTimestamp)` picks the formatter for a sink. At `LogLevel.Trace` it adds where the line was written. `FileSink` takes a formatter through its constructor. Your own sink implements `ILogSink` with a `MinimumLevel` and a `Write(in LogEvent, int indent)` method.

### Options

An options type is a class marked `[Options]`. `OptionsBinder.Deserialize` binds a JSONC document to it with camel case keys and refuses a key no member takes, so a typo fails instead of binding to nothing. A whole number reads as a JSON number or a `"0x"` hex string:

```cs
[Options]
public sealed class Settings
{
    [Choice(typeof(LogLevel))]
    public string? Level { get; set; }

    public required int Size { get; set; }

    public Output Output { get; set; } = new();
}

[Options]
public sealed class Output
{
    [Choice(typeof(OutputKind))]
    public string Kind { get; set; } = "console";

    [RequiredWhen("output.kind", "file")]
    public string? Path { get; set; }
}
```

```jsonc
{
  "level": "info",
  "size": "0x40",
  "output": { "kind": "file", "path": "logs/run.log" },
}
```

```cs
var settings = OptionsBinder.Deserialize<Settings>(jsonc, "$schema")!;

OptionsValidator.ValidateValues(settings, "settings.jsonc");
OptionsValidator.ThrowIfMissingRequired(settings, "settings.jsonc");
```

`[Choice]` accepts the kebab case name of each enum member, ignoring case, plus any `[ChoiceAlias]` on a member. `ValidateValues` runs every such mark and reports a bad value with the file and the option's path. `ThrowIfMissingRequired` checks every `required` member and every `[RequiredWhen]` whose deciding option has 1 of the listed values. `ColonKeys.Expand` turns an `"output:kind"` key into the nested form before binding. A check of your own derives from `OptionValidationAttribute` and overrides `Validate` and `WriteSchema`.

### Capabilities

An options document may lack a value because the build it describes has no such thing. `[Capability]` groups the options one feature reads. A document declares the feature unavailable with a reason. The required check then passes over those options:

```cs
[Options]
public sealed class Widget
{
    [Capability("draw")]
    public required int? Canvas { get; set; }
}

var unavailable = new Dictionary<string, UnavailableCapability>
{
    ["draw"] = new() { State = "not-applicable", Reason = "This build has no canvas." },
};

OptionsValidator.ThrowIfMissingRequired(widget, "widget.jsonc", unavailable);
UnavailableCapability.ThrowIfUnavailable(unavailable, "draw", "canvas");
```

`State` is `not-applicable` when the build lacks the thing or `unresolved` when no one has measured it yet. `UnavailableCapability.Validate` returns a `Result` with the reason instead of throwing. An option marked with 2 capabilities is only passed over when both are unavailable.

### Schema export

`OptionsSchemaExporter.Export` writes the JSON schema of an options type from the same contract the binder uses, so the schema and the binder never disagree about which keys exist:

```cs
var schema = OptionsSchemaExporter.Export(typeof(Settings), "Widget settings", allowColonKeys: true);

File.WriteAllText("settings.schema.json", OptionsSchemaExporter.Minify(schema));
```

A `[Description]` on a member becomes its `description`. A `[Choice]` lists its spellings, a whole number allows both of its spellings and a type with a public static `WriteSchema(Utf8JsonWriter)` or `WriteRules(Utf8JsonWriter)` method adds its own. A document points its editor at the file with `"$schema"`.

### Collections

A `LazyKeyedCollection<TKey, TValue>` finds items by key in a source that is slow to enumerate. A lookup enumerates the source only until it meets the key and caches every item it passes, so the next lookup for a cached key reads nothing:

```cs
public sealed class ModuleCollection(Process process)
    : LazyKeyedCollection<string, ProcessModule>(StringComparer.OrdinalIgnoreCase)
{
    public override IEnumerator<ProcessModule> GetEnumerator()
    {
        foreach (ProcessModule module in process.Modules)
        {
            yield return module;
        }
    }

    protected override bool TryGetKey(ProcessModule value, [MaybeNullWhen(false)] out string key)
    {
        key = value.ModuleName;

        return key is not null;
    }
}

var modules = new ModuleCollection(process);

if (modules.TryGetValue("game.exe", out var game))
{
    Console.WriteLine(game.BaseAddress);
}
```

`Clear` drops every cached item, so the next lookup reads the source again. `IReadOnlyKeyedCollection<TKey, TValue>` is the interface for a caller that only reads.

## Contributing

You need the .NET SDK version from `global.json`. These are the commands CI runs:

```
dotnet build
dotnet test
dotnet format --severity info --verify-no-changes
```

The `net4.8.1` tests only run on Windows.

## License

[0BSD](license.md)
