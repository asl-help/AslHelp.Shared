#!/usr/bin/env bash
# Writes what each command in the readme prints into expected/.
set -euo pipefail

dotnet options.cs > expected/settings.txt
! dotnet options.cs -- inputs/broken.jsonc 2> expected/broken.txt
dotnet options.cs -- --schema > expected/settings.schema.json
dotnet options.cs -- --help > expected/help.txt
