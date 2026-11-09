#!/bin/zsh
# Builds a self-contained Windows x64 CueAudacity.exe into ./build (works from macOS).
# On Windows, run build.cmd instead.
set -euo pipefail
cd "$(dirname "$0")"
dotnet publish CueAudacity/CueAudacity.csproj -c Release -o build
echo "Built $(cd build && pwd)/CueAudacity.exe"
