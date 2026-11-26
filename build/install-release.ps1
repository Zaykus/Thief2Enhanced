param(
    [string]$GameRoot = ''
)

$ErrorActionPreference = 'Stop'

# Release installer: installs the prebuilt binaries from this zip. It does not
# require .NET SDK, Visual Studio Build Tools or a compiler.
$modRoot = Split-Path -Parent $PSScriptRoot
if (-not $GameRoot) {
    # Works when the zip is extracted directly into the game folder.
    $candidate = Split-Path -Parent $modRoot
    if (Test-Path (Join-Path $candidate 'Thief Simulator 2.exe')) { $GameRoot = $candidate }
}
if (-not $GameRoot) {
    throw "Thief Simulator 2.exe was not found. Pass -GameRoot '<game folder>'."
}

$exe = Join-Path $GameRoot 'Thief Simulator 2.exe'
$managedDir = Join-Path $GameRoot 'Thief Simulator 2_Data\Managed'
if (-not (Test-Path $exe)) {
    throw "Thief Simulator 2.exe was not found in '$GameRoot'. Pass -GameRoot '<game folder>'."
}
if (-not (Test-Path (Join-Path $managedDir 'Assembly-CSharp.dll'))) {
    throw "Assembly-CSharp.dll was not found in '$managedDir'. Pass -GameRoot '<game folder>'."
}

$proxySource = Join-Path $modRoot 'native\UnityPlayer.dll'
$managedSource = Join-Path $modRoot 'bin\Thief2Enhanced.dll'
foreach ($file in @($proxySource, $managedSource)) {
    if (-not (Test-Path $file)) { throw "Prebuilt file is missing: $file" }
}

function Copy-WithRetry {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )
    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        try {
            Copy-Item -LiteralPath $Source -Destination $Destination -Force -ErrorAction Stop
            return
        }
        catch {
            if ($attempt -eq 9) { throw "Could not copy $Source to $Destination (file is locked)." }
            Start-Sleep -Milliseconds 500
        }
    }
}

Write-Host '== Installing prebuilt Thief2Enhanced =='
$engine = Join-Path $GameRoot 'UnityPlayer.dll'
$original = Join-Path $GameRoot 'UnityPlayer_Orig.dll'

if (-not (Test-Path $original)) {
    if (-not (Test-Path $engine)) { throw 'UnityPlayer.dll is missing from the game root.' }
    Copy-Item -LiteralPath $engine -Destination $original
    Write-Host 'Backed up original engine to UnityPlayer_Orig.dll'
}

$engineInstalled = $false
for ($attempt = 0; $attempt -lt 10; $attempt++) {
    try {
        Copy-Item -LiteralPath $proxySource -Destination $engine -Force -ErrorAction Stop
        $engineInstalled = $true
        break
    }
    catch {
        $engineItem = Get-Item -LiteralPath $engine -ErrorAction SilentlyContinue
        if ($engineItem -and $engineItem.Length -lt 100000) {
            Write-Host 'UnityPlayer.dll is already the proxy; keeping the running game intact.'
            $engineInstalled = $true
            break
        }
        Start-Sleep -Milliseconds 500
    }
}
if (-not $engineInstalled) { throw 'Could not install the native proxy (UnityPlayer.dll is locked).' }

Copy-WithRetry -Source $managedSource -Destination (Join-Path $managedDir 'Thief2Enhanced.dll')
# Remove the pre-rename assembly so Mono cannot load a stale copy.
Remove-Item -LiteralPath (Join-Path $managedDir 'ThiefSimulator2Mod.dll') -Force -ErrorAction SilentlyContinue

$configDir = Join-Path $GameRoot 'Mod\config'
New-Item -ItemType Directory -Force -Path $configDir, (Join-Path $GameRoot 'Mod\logs') | Out-Null
$configFile = Join-Path $configDir 'mod.cfg'
if (-not (Test-Path $configFile)) {
    Copy-Item -LiteralPath (Join-Path $modRoot 'config\mod.cfg') -Destination $configFile
}

Write-Host ''
Write-Host 'Installed Thief2Enhanced (prebuilt release).'
Write-Host '  Native proxy : UnityPlayer.dll -> UnityPlayer_Orig.dll'
Write-Host '  Managed mod  : Thief Simulator 2_Data\Managed\Thief2Enhanced.dll'
Write-Host '  Press F1 in game to open the menu.'
Write-Host '  Logs: Mod\logs\native-loader.log and Mod\logs\mod.log'
