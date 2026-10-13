param([string]$GameManagedDir = "", [string]$BepInExCoreDir = "")
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")
dotnet run --project tests/PolicyTests.csproj --configuration Release
if ($LASTEXITCODE -ne 0) { throw "Policy tests failed" }
$arguments = @("build", "src/MinecraftUltrakill/MinecraftUltrakill.csproj", "--configuration", "Release", "--nologo", "-warnaserror")
if ($GameManagedDir) {
    if (!$BepInExCoreDir) { throw "Specify -BepInExCoreDir with -GameManagedDir" }
    $arguments += "-p:GameManagedDir=$GameManagedDir"
    $arguments += "-p:BepInExCoreDir=$BepInExCoreDir"
}
dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw "Mod build failed" }
python scripts/package.py src/MinecraftUltrakill/bin/Release/net472/MinecraftUltrakill.dll
if ($LASTEXITCODE -ne 0) { throw "Packaging failed" }
python scripts/validate-package.py release/MinecraftUltrakill-0.1.0.zip
if ($LASTEXITCODE -ne 0) { throw "Package validation failed" }
