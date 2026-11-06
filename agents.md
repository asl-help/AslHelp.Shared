# agents.md

## Commands

```
just build
just test
just format
just encoding
just check
```

`just check` runs the encoding check, the format check, the build and the tests, as CI does. Run it before a commit.

The `net4.8.1` tests only run on Windows.

## Rules

- Text files are UTF-8 without a BOM. Git stores them with LF line endings.
- `readme.md`, `changelog.md` and this file change in the same commit as the code they describe.
- Text a user reads goes in the project's `SR.resx`, sorted by key. Keep the generated class at `AslHelp.Shared.SR`, because ILRepack renames a second `SR` with the same full name and its lookup then fails.
- Commit subjects are lowercase and imperative, with no body and no trailers.

## Samples

Each sample lives in `samples/<name>/` and is a single-file app whose entry point is `<name>.cs`, starting with `#!/usr/bin/env dotnet`. Its `expected.sh` runs the commands from its readme and writes their output into `expected/`. CI formats every entry point, runs every `expected.sh` and fails when `expected/` changes. A new sample also gets a `<Folder>` in `AslHelp.Shared.slnx`.
