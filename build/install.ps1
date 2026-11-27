param(
    [string]$GameRoot = ''
)

 $ErrorActionPreference = 'Stop'

 $modRoot = $PSScriptRoot


if (-not $GameRoot) {
    $candidates = @(
        $modRoot,
        (Split-Path -Parent $modRoot),
        (Split-Path -Parent (Split-Path -Parent $modRoot))
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath (Join-Path $candidate 'Thief Simulator 2.exe'))) {
            $GameRoot = $candidate
            break
        }
    }
}
if (-not $GameRoot) {
    throw "Thief Simulator 2.exe was not found. Put this file inside the game folder or pass -GameRoot '<game folder>'."
}

 $exe = Join-Path $GameRoot 'Thief Simulator 2.exe'
 $managedDir = Join-Path $GameRoot 'Thief Simulator 2_Data\Managed'
if (-not (Test-Path -LiteralPath $exe)) {
    throw "Thief Simulator 2.exe was not found in '$GameRoot'. Pass -GameRoot '<game folder>'."
}
if (-not (Test-Path -LiteralPath (Join-Path $managedDir 'Assembly-CSharp.dll'))) {
    throw "Assembly-CSharp.dll was not found in '$managedDir'. Pass -GameRoot '<game folder>'."
}

 $proxySource = Join-Path $modRoot 'UnityPlayer.dll'
 $managedSource = Join-Path $modRoot 'Thief2Enhanced.dll'
foreach ($file in @($proxySource, $managedSource)) {
    if (-not (Test-Path -LiteralPath $file)) { throw "Mod file is missing from the package: $file" }
}

function Test-IsProxyPath {
    param([Parameter(Mandatory = $true)] [string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    return ((Get-Item -LiteralPath $Path).Length -lt 100000)
}

function Copy-WithRetry {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )
    if ([System.IO.Path]::GetFullPath($Source) -ieq [System.IO.Path]::GetFullPath($Destination)) {
        Write-Host "Already in place: $Destination"
        return
    }
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

Write-Host '== Installing Thief2Enhanced =='
 $engine = Join-Path $GameRoot 'UnityPlayer.dll'
 $original = Join-Path $GameRoot 'UnityPlayer_Orig.dll'

if (-not (Test-Path -LiteralPath $original)) {
    if (-not (Test-Path -LiteralPath $engine)) { throw 'UnityPlayer.dll is missing from the game root.' }
    if (Test-IsProxyPath $engine) {
        throw @'
The game's original UnityPlayer.dll was already overwritten and no UnityPlayer_Orig.dll backup exists.
Restore the engine first: Steam -> right-click Thief Simulator 2 -> Properties -> Installed Files ->
"Verify integrity of game files", then run INSTALL.bat again.
'@
    }
    Copy-Item -LiteralPath $engine -Destination $original
    Write-Host 'Backed up original engine to UnityPlayer_Orig.dll'
}

 $engineInstalled = $false
 $samePath = [System.IO.Path]::GetFullPath($proxySource) -ieq [System.IO.Path]::GetFullPath($engine)
if ($samePath) {
    Write-Host 'UnityPlayer.dll is already the mod proxy (flat package); skipping copy.'
    $engineInstalled = $true
}
else {
    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        try {
            Copy-Item -LiteralPath $proxySource -Destination $engine -Force -ErrorAction Stop
            $engineInstalled = $true
            break
        }
        catch {
            $engineItem = Get-Item -LiteralPath $engine -ErrorAction SilentlyContinue
            if ($engineItem -and ($engineItem.Length -lt 100000)) {
                Write-Host 'UnityPlayer.dll is already the proxy; keeping the running game intact.'
                $engineInstalled = $true
                break
            }
            if ($attempt -eq 9) { throw 'Could not install the native proxy (UnityPlayer.dll is locked).' }
            Start-Sleep -Milliseconds 500
        }
    }
}
if (-not $engineInstalled) { throw 'Could not install the native proxy (UnityPlayer.dll is locked).' }

Copy-WithRetry -Source $managedSource -Destination (Join-Path $managedDir 'Thief2Enhanced.dll')
Remove-Item -LiteralPath (Join-Path $managedDir 'ThiefSimulator2Mod.dll') -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path (Join-Path $GameRoot 'Mod\logs') | Out-Null

Write-Host ''
Write-Host 'Installed Thief2Enhanced.'
Write-Host '  Native proxy : UnityPlayer.dll -> UnityPlayer_Orig.dll'
Write-Host '  Managed mod  : Thief Simulator 2_Data\Managed\Thief2Enhanced.dll'
Write-Host '  Press F1 in game to open the menu.'
Write-Host '  Logs: Mod\logs\native-loader.log and Mod\logs\mod.log'
