# Better Hydration & Energy HUD - local deploy helper
#
# Usage:
#   .\deploy.ps1 -Mode Workshop                  # test in-game by replacing the Workshop copy (backup kept)
#   .\deploy.ps1 -Mode Local                     # install as a local mod (no Workshop dependency)
#   .\deploy.ps1 -Mode Restore -Backup <dir>     # restore a previous backup
#
# The Workshop folder is managed by Steam; a Steam "verify files" or a Workshop update of
# this item will restore the old files. The backup made here always allows a rollback.

param(
    [ValidateSet("Workshop", "Local", "Restore")][string]$Mode = "Workshop",
    [string]$GamePath = "",
    [string]$Backup = "",
    [string]$Source = ""
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$WorkshopId = "3591875771"

function Resolve-GamePath {
    if ($GamePath -ne "") { if (-not (Test-Path $GamePath)) { throw "Game path not found: $GamePath" }; return $GamePath }
    if ($env:DUCKOV_GAME -and (Test-Path $env:DUCKOV_GAME)) { return $env:DUCKOV_GAME }
    $candidates = @(
        "C:\Program Files (x86)\Steam\steamapps\common\Escape from Duckov",
        "C:\Program Files\Steam\steamapps\common\Escape from Duckov",
        "D:\Steam\steamapps\common\Escape from Duckov",
        "D:\SteamLibrary\steamapps\common\Escape from Duckov"
    )
    foreach ($c in $candidates) { if (Test-Path $c) { return $c } }
    throw "Escape from Duckov not found. Pass -GamePath."
}

function Resolve-WorkshopDir {
    param([string]$game)
    $steamApps = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $game))
    $candidate = Join-Path $steamApps "workshop\content\3167020\$WorkshopId"
    if (Test-Path $candidate) { return $candidate }
    $guess = Join-Path (Split-Path -Parent $game) "..\workshop\content\3167020\$WorkshopId"
    if (Test-Path $guess) { return (Resolve-Path $guess).Path }
    throw "Workshop content folder not found for item $WorkshopId."
}

$game = Resolve-GamePath

if ($Mode -eq "Restore") {
    if ($Backup -eq "" -or -not (Test-Path $Backup)) { throw "Pass -Backup <backup folder>" }
    $target = Resolve-WorkshopDir $game
    Get-ChildItem $Backup -File | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $target $_.Name) -Force
        Write-Host "restored $($_.Name)"
    }
    Write-Host "Restore done." -ForegroundColor Green
    exit 0
}

if ($Source -eq "") { $Source = Join-Path $RepoRoot "dist\BetterHydrationEnergyHUD" }
if (-not (Test-Path (Join-Path $Source "BetterHydrationEnergyHUD.dll"))) {
    throw "No build found in $Source. Run .\build.ps1 first."
}

if ($Mode -eq "Workshop") {
    $target = Resolve-WorkshopDir $game
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupDir = Join-Path $RepoRoot "backups\workshop-$stamp"
    New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
    foreach ($name in @("BetterHydrationEnergyHUD.dll", "info.ini", "settings.ini")) {
        $old = Join-Path $target $name
        if (Test-Path $old) { Copy-Item $old (Join-Path $backupDir $name) }
    }
    Copy-Item (Join-Path $Source "BetterHydrationEnergyHUD.dll") (Join-Path $target "BetterHydrationEnergyHUD.dll") -Force
    Copy-Item (Join-Path $Source "info.ini") (Join-Path $target "info.ini") -Force
    $preview = Join-Path $Source "preview.png"
    if (Test-Path $preview) { Copy-Item $preview (Join-Path $target "preview.png") -Force }
    Write-Host "Deployed to Workshop: $target" -ForegroundColor Green
    Write-Host "Backup: $backupDir"
} else {
    $target = Join-Path $game "Duckov_Data\Mods\BetterHydrationEnergyHUD"
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    Copy-Item (Join-Path $Source "BetterHydrationEnergyHUD.dll") (Join-Path $target "BetterHydrationEnergyHUD.dll") -Force
    Copy-Item (Join-Path $Source "info.ini") (Join-Path $target "info.ini") -Force
    Write-Host "Installed local mod: $target" -ForegroundColor Green
    Write-Host "NOTE: disable the Workshop copy of the same mod in the in-game Mods list to avoid a name collision."
}
