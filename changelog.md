# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/2.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `Unwrap`, `Expect`, `UnwrapOr`, `UnwrapOrDefault`, `UnwrapOrElse`, `TryUnwrap`, `UnwrapErr`, `ExpectErr` and `TryUnwrapErr` for reading a result's value or error.
- `Map`, `MapErr`, `MapOr`, `MapOrElse` and `Match` for turning a result into another result or a value.
- `And`, `AndThen`, `Or` and `OrElse` for chaining results.
- `Inspect` and `InspectErr` for running an action on a result without changing it.
- `LazyKeyedCollection<TKey, TValue>` and `IReadOnlyKeyedCollection<TKey, TValue>` for finding items by key in a source that is enumerated lazily and cached.
- `Logger` for writing log lines to sinks, with `ILogSink`, `LogEvent`, `LogLevel` and the `Plain`, `Stamped`, `Located` and `Traced` formatters.
- `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning` and `LogError` for writing an interpolated message that is only composed when a sink writes at that level, plus `Log` and `BeginScope` that take a level and a message.

## [0.1.0-alpha.1] - 2026-10-22

### Added
- `Result` and `Result<TValue>` for operations that fail without throwing, with `ResultError` as the base type for their errors.
- `ExceptionError` for returning a caught exception as a failure.

[Unreleased]: https://github.com/asl-help/AslHelp.Shared/compare/0.1.0-alpha.1...HEAD
[0.1.0-alpha.1]: https://github.com/asl-help/AslHelp.Shared/releases/tag/0.1.0-alpha.1
