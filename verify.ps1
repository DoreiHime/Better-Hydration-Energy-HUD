# Better Hydration & Energy HUD - build verification
#
# Checks that the produced DLL is a valid, dependency-free Duckov mod:
#   * assembly name is BetterHydrationEnergyHUD
#   * contains a public type "BetterHydrationEnergyHUD.ModBehaviour"
#     that extends Duckov.Modding.ModBehaviour (verified through IL)
#   * only references assemblies shipped with the game (zero third-party
#     prerequisites: no ModSetting / ModConfig / Harmony / BepInEx)
#   * has a sane size

param(
    [Parameter(Mandatory = $true)][string]$Dll
)

$ErrorActionPreference = "Stop"
$failures = New-Object System.Collections.Generic.List[string]

if (-not (Test-Path $Dll)) { throw "DLL not found: $Dll" }
$size = (Get-Item $Dll).Length
Write-Host "DLL: $Dll ($size bytes)"
if ($size -lt 20000) { $failures.Add("DLL looks too small ($size bytes)") }

$ilspy = Join-Path $env:USERPROFILE ".dotnet\tools\ilspycmd.exe"
if (-not (Test-Path $ilspy)) {
    $cmd = Get-Command ilspycmd -ErrorAction SilentlyContinue
    if ($cmd) { $ilspy = $cmd.Source }
}
if (-not (Test-Path $ilspy)) {
    $failures.Add("ilspycmd not found; cannot verify types. Install with: dotnet tool install -g ilspycmd")
} else {
    $types = (& $ilspy -l c $Dll 2>&1 | Out-String)
    if ($types -notmatch "Class BetterHydrationEnergyHUD\.ModBehaviour") {
        $failures.Add("Type BetterHydrationEnergyHUD.ModBehaviour not found")
    } else {
        Write-Host "OK: BetterHydrationEnergyHUD.ModBehaviour found"
    }

    $il = (& $ilspy --ilcode -t "BetterHydrationEnergyHUD.ModBehaviour" $Dll 2>&1 | Out-String)
    if ($il -notmatch "extends \[TeamSoda\.Duckov\.Core\]Duckov\.Modding\.ModBehaviour") {
        $failures.Add("ModBehaviour does not extend Duckov.Modding.ModBehaviour")
    } else {
        Write-Host "OK: entry point extends Duckov.Modding.ModBehaviour"
    }

    # Assembly references: only the game's own assemblies are allowed.
    $refs = (& $ilspy --dump-table AssemblyRef $Dll 2>&1 | Out-String)
    $allowedExact = @("netstandard", "mscorlib", "System", "System.Core", "TeamSoda.Duckov.Core", "TeamSoda.Duckov.Utilities", "ItemStatsSystem", "SodaLocalization", "Plugins", "Unity.TextMeshPro", "Unity.InputSystem", "UnityEngine.UI", "DynLib")
    $bad = New-Object System.Collections.Generic.List[string]
    foreach ($line in ($refs -split "`r?`n")) {
        $m = [regex]::Match($line, '^\d+\s+0x[0-9A-Fa-f]+\s+[\d.]+\s+\S+\s+\S+\s+(\S+)')
        if (-not $m.Success) { continue }
        $name = $m.Groups[1].Value
        $ok = ($allowedExact -contains $name) -or $name.StartsWith("UnityEngine.") -or $name.StartsWith("System.")
        if (-not $ok) { $bad.Add($name) }
    }
    if ($bad.Count -gt 0) {
        foreach ($b in $bad) { $failures.Add("Unexpected assembly reference: $b") }
    } else {
        Write-Host "OK: dependency free (only game / Unity / netstandard references)"
    }
}

if ($failures.Count -gt 0) {
    Write-Host ""
    foreach ($f in $failures) { Write-Host "FAIL: $f" -ForegroundColor Red }
    exit 1
}
Write-Host ""
Write-Host "Verification passed." -ForegroundColor Green
exit 0
