# Better Hydration & Energy HUD - build script
#
# Usage:
#   .\build.ps1                              # build only (dist/BetterHydrationEnergyHUD)
#   .\build.ps1 -DeployWorkshop              # + copy into the local Steam Workshop folder (backup kept)
#   .\build.ps1 -DeployLocal                 # + install as a local mod in Duckov_Data\Mods
#   .\build.ps1 -GamePath "D:\Steam\...\Escape from Duckov"
#
# Requires: .NET SDK 8/9/10 and an installed copy of Escape from Duckov.

param(
    [string]$Configuration = "Release",
    [string]$GamePath = "",
    [switch]$DeployWorkshop,
    [switch]$DeployLocal
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $RepoRoot "src\BetterHydrationEnergyHUD"
$ProjectFile = Join-Path $ProjectDir "BetterHydrationEnergyHUD.csproj"
$DistDir = Join-Path $RepoRoot "dist\BetterHydrationEnergyHUD"
$WorkshopId = "3591875771"

function Write-Step($message) {
    Write-Host ""
    Write-Host "==> $message" -ForegroundColor Cyan
}

function Resolve-GamePath {
    if ($GamePath -ne "") {
        if (-not (Test-Path $GamePath)) { throw "Game path not found: $GamePath" }
        return $GamePath
    }
    $envGame = $env:DUCKOV_GAME
    if ($envGame -and (Test-Path $envGame)) { return $envGame }
    $candidates = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov",
        "C:\Program Files\Steam\steamapps\common\Escape from Duckov",
        "D:\Steam\steamapps\common\Escape from Duckov",
        "D:\SteamLibrary\steamapps\common\Escape from Duckov",
        "E:\SteamLibrary\steamapps\common\Escape from Duckov"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    throw "Escape from Duckov not found. Pass -GamePath or set DUCKOV_GAME."
}

$game = Resolve-GamePath
$managed = Join-Path $game "Duckov_Data\Managed"
if (-not (Test-Path (Join-Path $managed "TeamSoda.Duckov.Core.dll"))) {
    throw "Game assemblies not found under: $managed"
}

Write-Step "Building $Configuration"
& dotnet build $ProjectFile -c $Configuration -p:GameManagedDir="$managed" --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)" }

$dll = Join-Path $ProjectDir "bin\$Configuration\BetterHydrationEnergyHUD.dll"
if (-not (Test-Path $dll)) { throw "Build output not found: $dll" }

Write-Step "Assembling dist package"
if (Test-Path $DistDir) { Remove-Item $DistDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $DistDir | Out-Null
Copy-Item $dll (Join-Path $DistDir "BetterHydrationEnergyHUD.dll")
Copy-Item (Join-Path $ProjectDir "info.ini") (Join-Path $DistDir "info.ini")
$preview = Join-Path $RepoRoot "assets\preview.png"
if (Test-Path $preview) { Copy-Item $preview (Join-Path $DistDir "preview.png") }
$license = Join-Path $RepoRoot "LICENSE"
if (Test-Path $license) { Copy-Item $license (Join-Path $DistDir "LICENSE.txt") }

Write-Step "Verifying build"
& (Join-Path $RepoRoot "verify.ps1") -Dll (Join-Path $DistDir "BetterHydrationEnergyHUD.dll")
if ($LASTEXITCODE -ne 0) { throw "verify.ps1 failed" }

Write-Host ""
Write-Host "Package ready: $DistDir" -ForegroundColor Green
Get-ChildItem $DistDir | Select-Object Name, Length | Format-Table -AutoSize

if ($DeployWorkshop -or $DeployLocal) {
    & (Join-Path $RepoRoot "deploy.ps1") -GamePath $game -Mode $(if ($DeployWorkshop) { "Workshop" } else { "Local" }) -Source $DistDir
}
