param(
    [string]$GameRoot = ''
)

$ErrorActionPreference = 'Stop'

$modRoot = Split-Path -Parent $PSScriptRoot
if (-not $GameRoot) { $GameRoot = Split-Path -Parent $modRoot }

$engine = Join-Path $GameRoot 'UnityPlayer.dll'
$original = Join-Path $GameRoot 'UnityPlayer_Orig.dll'

if (Test-Path $original) {
    Copy-Item -LiteralPath $original -Destination $engine -Force
    Remove-Item -LiteralPath $original -Force
    Write-Host 'Restored original UnityPlayer.dll'
}

$managed = Join-Path $GameRoot 'Thief Simulator 2_Data\Managed'
Remove-Item -LiteralPath (Join-Path $managed 'Thief2Enhanced.dll') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $managed 'ThiefSimulator2Mod.dll') -Force -ErrorAction SilentlyContinue
# These libraries are only used by this mod; keep other Harmony copies if present.
Remove-Item -LiteralPath (Join-Path $managed '0Harmony.dll') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $managed 'Mono.Cecil.dll') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $managed 'MonoMod.Utils.dll') -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $managed 'MonoMod.RuntimeDetour.dll') -Force -ErrorAction SilentlyContinue

Write-Host 'Thief2Enhanced uninstalled.'
