# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/2.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-alpha.2] - 2026-10-23

### Added
- `Unwrap`, `Expect`, `UnwrapOr`, `UnwrapOrDefault`, `UnwrapOrElse`, `TryUnwrap`, `UnwrapErr`, `ExpectErr` and `TryUnwrapErr` for reading a result's value or error.
- `Map`, `MapErr`, `MapOr`, `MapOrElse` and `Match` for turning a result into another result or a value.
- `And`, `AndThen`, `Or` and `OrElse` for chaining results.
- `Inspect` and `InspectErr` for running an action on a result without changing it.
- `LazyKeyedCollection<TKey, TValue>` and `IReadOnlyKeyedCollection<TKey, TValue>` for finding items by key in a source that is enumerated lazily and cached.
- `Logger` for writing log lines to sinks, with `ILogSink`, `LogEvent`, `LogLevel` and the `Plain`, `Stamped`, `Located` and `Traced` formatters.
- `LogTrace`, `LogDebug`, `LogInformation`, `LogWarning` and `LogError` for writing an interpolated message that is only composed when a sink writes at that level, plus `Log` and `BeginScope` that take a level and a message.
- `FileSink` for writing log lines to a file on a separate thread, erasing the oldest lines once the file gets too long.
- `LogFormatter.ForLevel` for picking the formatter that fits a sink's lowest level.
- `Choice` and `ChoiceAliasAttribute` for reading an enum from its kebab-case name or an alias.
- `HexNumber` and `HexNumberConverter` for reading a number written as `0x` hex.
- `OptionsBinder` for binding a JSONC document to an options type, reading a validated profile with `ParseProfile` and overlaying one bound object onto another.
- `OptionsContract` for reading the members an options type binds and the values under them.
- `OptionsValidator` for checking required leaves, marked values and document keys.
- `OptionsSchemaExporter` for exporting a JSON schema of an options type, with each member's `[Description]`.
- `CapabilityAttribute` and `UnavailableCapability` for options an entry may leave out when it declares the capability that reads them unavailable.
- `ProfileCatalogue<TKey, TOptions>` and `VersionedProfileCatalogue<TOptions>` for registering profiles and choosing the newest one at or below a target's version, with `ProfileKey` as the key.
- `ProfileVersions` for ordering engine versions the way Unity orders its releases.
- `ProfileFallback` for warning about versions that use the data of a nearby entry.
- `EngineVersionAttribute` for checking that an option is an engine version.

## [0.1.0-alpha.1] - 2026-10-22

### Added
- `Result` and `Result<TValue>` for operations that fail without throwing, with `ResultError` as the base type for their errors.
- `ExceptionError` for returning a caught exception as a failure.

[Unreleased]: https://github.com/asl-help/AslHelp.Shared/compare/0.1.0-alpha.2...HEAD
[0.1.0-alpha.2]: https://github.com/asl-help/AslHelp.Shared/compare/0.1.0-alpha.1...0.1.0-alpha.2
[0.1.0-alpha.1]: https://github.com/asl-help/AslHelp.Shared/releases/tag/0.1.0-alpha.1
