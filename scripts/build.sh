#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet run --project tests/PolicyTests.csproj --configuration Release
dotnet build src/MinecraftUltrakill/MinecraftUltrakill.csproj --configuration Release --nologo -warnaserror "$@"
python3 scripts/package.py src/MinecraftUltrakill/bin/Release/net472/MinecraftUltrakill.dll
python3 scripts/validate-package.py release/MinecraftUltrakill-0.1.0.zip
