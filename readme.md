# AslHelp.Shared

AslHelp.Shared is a small .NET library with `Result` types for operations that fail without throwing.

## Install

```
dotnet add package AslHelp.Shared
```

## Usage

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

var result = ReadLevel(-1);
if (result.IsErr)
{
    Console.WriteLine(result.Error.Message);
}
```

`Result` works the same way for operations that don't return a value.

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
