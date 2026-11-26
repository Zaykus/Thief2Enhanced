param(
    [string]$GameRoot = ''
)

$ErrorActionPreference = 'Stop'

$modRoot = Split-Path -Parent $PSScriptRoot
if (-not $GameRoot) { $GameRoot = Split-Path -Parent $modRoot }

$exe = Join-Path $GameRoot 'Thief Simulator 2.exe'
if (-not (Test-Path $exe)) {
    throw "Thief Simulator 2.exe was not found in '$GameRoot'. Pass -GameRoot '<game folder>'."
}

$nativeDir = Join-Path $modRoot 'native'
$mergeScript = Join-Path $PSScriptRoot 'merge.ps1'
$managedDir = Join-Path $GameRoot 'Thief Simulator 2_Data\Managed'
if (-not (Test-Path (Join-Path $managedDir 'Assembly-CSharp.dll'))) {
    throw "Assembly-CSharp.dll was not found in '$managedDir'. Pass -GameRoot '<game folder>'."
}

function Copy-WithRetry {
    param(
        [Parameter(Mandatory = $true)] [string] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination
    )
    for ($attempt = 0; $attempt -lt 10; $attempt++)
    {
        try
        {
            Copy-Item -LiteralPath $Source -Destination $Destination -Force -ErrorAction Stop
            return
        }
        catch
        {
            if ($attempt -eq 9) { throw "Could not copy $Source to $Destination (file is locked)." }
            Start-Sleep -Milliseconds 500
        }
    }
}

Write-Host '== Building native loader =='
& (Join-Path $nativeDir 'build.ps1')

Write-Host '== Building Thief2Enhanced =='
# merge.ps1 builds the managed project and folds Harmony/MonoMod into the DLL.
& $mergeScript -GameRoot $GameRoot
if ($LASTEXITCODE -ne 0) { throw 'Merged managed build failed.' }

Write-Host '== Installing =='
$engine = Join-Path $GameRoot 'UnityPlayer.dll'
$original = Join-Path $GameRoot 'UnityPlayer_Orig.dll'

if (-not (Test-Path $original)) {
    if (-not (Test-Path $engine)) { throw 'UnityPlayer.dll is missing from the game root.' }
    Copy-Item -LiteralPath $engine -Destination $original
    Write-Host 'Backed up original engine to UnityPlayer_Orig.dll'
}

# The game may still be running; retry the locked engine DLL copy. If it is
# already our small proxy, continuing is safe (the managed DLLs are rebuilt).
$proxySource = Join-Path $nativeDir 'UnityPlayer.dll'
$engineInstalled = $false
for ($attempt = 0; $attempt -lt 10; $attempt++)
{
    try
    {
        Copy-Item -LiteralPath $proxySource -Destination $engine -Force -ErrorAction Stop
        $engineInstalled = $true
        break
    }
    catch
    {
        $engineItem = Get-Item -LiteralPath $engine -ErrorAction SilentlyContinue
        if ($engineItem -and $engineItem.Length -lt 100000)
        {
            Write-Host 'UnityPlayer.dll is already the proxy; keeping the running game intact.'
            $engineInstalled = $true
            break
        }
        Start-Sleep -Milliseconds 500
    }
}
if (-not $engineInstalled) { throw 'Could not install the native proxy (UnityPlayer.dll is locked).' }

Copy-WithRetry -Source (Join-Path $modRoot 'src\ThiefSimulator2Mod\bin\Release\Thief2Enhanced.dll') -Destination (Join-Path $managedDir 'Thief2Enhanced.dll')
# Remove the pre-rename assembly so Mono cannot load a stale copy.
Remove-Item -LiteralPath (Join-Path $managedDir 'ThiefSimulator2Mod.dll') -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Force -Path (Join-Path $GameRoot 'Mod\config'), (Join-Path $GameRoot 'Mod\logs') | Out-Null

Write-Host ''
Write-Host 'Installed Thief2Enhanced.'
Write-Host '  Native proxy : UnityPlayer.dll -> UnityPlayer_Orig.dll'
Write-Host '  Managed mod  : Thief Simulator 2_Data\Managed\Thief2Enhanced.dll'
Write-Host '  Press F1 in game to open the menu.'
Write-Host '  Logs: Mod\logs\native-loader.log and Mod\logs\mod.log'
