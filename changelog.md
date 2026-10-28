# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/2.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- **Breaking:** `ILogSink.Emit` -> `ILogSink.Write` on every sink, `FileSink` included. Its `e` parameter is now `logEvent`, as on `LogFormatter` and `Logger.Log`.
- **Breaking:** `Logger.Add` -> `Logger.AddSink` and the `where` parameter of `Logger.GetSink` -> `predicate`.
- **Breaking:** `LogFormatter.ForLevel` -> `LogFormatter.Create`, with `stamped` -> `includeTimestamp`. The formatters `Plain`, `Stamped`, `Located` and `Traced` -> `FormatPlain`, `FormatWithTimestamp`, `FormatWithLocation` and `FormatWithTimestampAndLocation`.
- **Breaking:** `OptionCheckAttribute` -> `OptionValidationAttribute`, with `Check` -> `Validate` on it and on `ChoiceAttribute` and `EngineVersionAttribute`.
- **Breaking:** `ChoiceAliasAttribute.Spelling` -> `ChoiceAliasAttribute.Alias`. `Choice.Spellings` -> `Choice.GetNames`, `Choice.Aliases` -> `Choice.GetAliases` and the `option` parameter of `Choice.Parse` -> `path`.
- **Breaking:** the `named` parameter of `HexNumber.Parse` and `HexNumber.Read` -> `paramName`.
- **Breaking:** `UnavailableCapability.Check` -> `UnavailableCapability.Validate` and `UnavailableCapability.Require` -> `UnavailableCapability.ThrowIfUnavailable`.
- **Breaking:** `OptionsValidator.Require` -> `ThrowIfMissingRequired`, `Unset` -> `GetUnsetKeys`, `CheckKeys` -> `ValidateKeys` and `CheckValues` -> `ValidateValues`. The `owner` parameter of `ValidateKeys` is now `objectName`.
- **Breaking:** `OptionsContract.Members` -> `GetMembers`, `Member` -> `GetMember`, `Read` -> `GetValue` and `Capabilities` -> `GetCapabilities`. `TryMember` -> `TryGetMember`, which returns a `bool` and sets the member through an `out` parameter.
- **Breaking:** `OptionsBinder.Bind` -> `OptionsBinder.Deserialize`, `EnumerateShipped` -> `EnumerateEmbeddedProfiles` and `Overlay` -> `ApplyOverrides`.
- **Breaking:** `ProfileFallback.UsingDataFrom` -> `ProfileFallback.SourceVersion` and `ProfileFallback.Covers` -> `ProfileFallback.Contains`. A fallback document still writes the key as `usingDataFrom`.
- **Breaking:** `ProfileKey.LaneOf` -> `ProfileKey.GetArchitectureName` and `ProfileKey.TryParseShippedName` -> `ProfileKey.TryParseResourceName`.
- **Breaking:** `ProfileCatalogue.DocumentOf` -> `GetDocument`, `Has` -> `ContainsKey`, `Describe` -> `GetDisplayName` and `WarnOnFallback` -> `LogFallbackWarning`. The `plant` parameter of `Resolve` is now `configure`. The `log`, `lane` and `laneLabel` parameters are now `logger`, `architecture` and `architectureLabel`.
- **Breaking:** `VersionedProfileCatalogue.Keyed` -> `CreateKey`, `LoadShipped` -> `LoadEmbedded`, `AddShipped` -> `AddEmbedded`, `TryChoose` -> `TryFindKey` and `NoProfile` -> `CreateNoProfileError`. `ResolveChosen` is now a `Resolve` overload. Their `chosen`, `applies`, `log`, `lane`, `laneLabel`, `note` and `plant` parameters are now `key`, `predicate`, `logger`, `architecture`, `architectureLabel`, `message` and `configure`.
- **Breaking:** `ProfileVersions.Check` -> `Validate`, `Components` -> `GetComponents`, `ReleaseLine` -> `GetReleaseLine` and `TryNewestAtOrBelow` -> `TryFindLatestAtOrBelow`, with `versionOf` -> `versionSelector` and `chosen` -> `key`. The parameters of `ProfileVersions.Compare` are now `left` and `right`.
- **Breaking:** `OptionsSchemaExporter.Minified` -> `OptionsSchemaExporter.Minify` and the `colonKeys` parameter of `Export` -> `allowColonKeys`.

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
