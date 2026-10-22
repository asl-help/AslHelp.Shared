# AslHelp.Shared

AslHelp.Shared is a small .NET library with `Result` types for operations that fail without throwing.

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
