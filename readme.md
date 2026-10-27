# AslHelp.Shared

AslHelp.Shared is a small .NET library with the pieces the other AslHelp packages share. It has `Result` types for operations that fail without throwing, a `Logger` with sinks, options that bind from JSONC with checks and a schema, profile catalogs keyed by version and a lazy keyed collection.

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

A profile may lack a value because the build it describes has no such thing. `[Capability]` groups the options one feature reads. A document declares the feature unavailable with a reason. The required check then passes over those options:

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

OptionsValidator.ThrowIfMissingRequired(widget, "The profile", unavailable);
UnavailableCapability.ThrowIfUnavailable(unavailable, "draw", "canvas");
```

`State` is `not-applicable` when the build lacks the thing or `unresolved` when no one has measured it yet. `UnavailableCapability.Validate` returns a `Result` with the reason instead of throwing. An option marked with 2 capabilities is only passed over when both are unavailable.

### Profiles

A profile is an options document measured against 1 build. A `VersionedProfileCatalogue<TOptions>` keeps profiles by version and pointer size and picks the newest entry at or below a target's version. You derive from it to give the engine a label and to expose a lookup:

```cs
public sealed class WidgetCatalogue : VersionedProfileCatalogue<WidgetProfile>
{
    public WidgetCatalogue()
        : base("Widget", "widget")
    {
        LoadEmbedded(typeof(WidgetCatalogue).Assembly, "Profiles/Widget", partitioned: false, fallbacks: "widget");
    }

    public Result<WidgetProfile> Resolve(string version, int pointerSize, WidgetProfile? overrides, Logger? logger)
    {
        if (!TryFindKey("", version, pointerSize, out var key))
        {
            return CreateNoProfileError("", version, pointerSize);
        }

        string architecture = ProfileKey.GetArchitectureName(pointerSize);

        return Resolve(key, version, architecture, $"Widget {architecture}", overrides, logger);
    }

    protected override WidgetProfile Parse(string document)
    {
        return OptionsBinder.ParseProfile<WidgetProfile>(document, Validate, "$schema");
    }

    protected override void Validate(WidgetProfile profile, string origin)
    {
        OptionsValidator.ThrowIfMissingRequired(profile, origin);
    }
}
```

`LoadEmbedded` reads every `.jsonc` the assembly embeds under the folder. A file is called `<version>-<x86|x64>.jsonc`, such as `Profiles/Widget/2019.4-x64.jsonc`. A partitioned catalog reads `<partition>-<version>-<x86|x64>.jsonc` instead. Every profile is validated as it enters the catalog, so a broken profile fails at startup instead of at its first use. `Resolve` with overrides copies the caller's values over a fresh parse of the entry and returns a failed `Result` when they break the profile.

A fallback is an interval of versions served with data from another version. `LoadEmbedded` also reads `Profiles/Fallbacks/<fallbacks>.jsonc` and `Resolve` writes a warning when the target falls inside a fallback:

```jsonc
{
  "x64": [
    { "from": "2019.4.0f1", "until": "2019.4.10f1", "usingDataFrom": "2019.4.10f1" }
  ]
}
```

`ProfileVersions` orders versions the way Unity does, so `2019.4.0a2` comes before `2019.4.0f1`. `[EngineVersion]` checks that a string option is spelled like one. `ProfileCatalogue<TKey, TOptions>` is the base for a catalog with another kind of key.

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
