# Options sample

This sample reads a JSONC settings document into the `Settings` class, checks it and prints what it bound. It's a single-file app, so it runs without a project file.

- [Settings.cs](Settings.cs) declares the options.
- [inputs](inputs) has the documents the sample reads. [settings.jsonc](inputs/settings.jsonc) has comments and a trailing comma. `"0x40"` reads as 64. The `output:kind` key sets `kind` inside the `output` section.
- [expected](expected) has what each command below prints. [expected.sh](expected.sh) runs the commands and writes their output there. CI runs it and fails when the output differs.

| Command | Output |
|---|---|
| `dotnet options.cs` | [settings.txt](expected/settings.txt) |
| `dotnet options.cs -- inputs/broken.jsonc` | [broken.txt](expected/broken.txt), with exit code 1 |
| `dotnet options.cs -- --schema` | [settings.schema.json](expected/settings.schema.json), which the inputs point their `$schema` at |
| `dotnet options.cs -- --help` | [help.txt](expected/help.txt) |

Run the commands from this folder.
