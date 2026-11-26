$ErrorActionPreference = 'Stop'

# Locate the newest MSVC toolset and Windows 10/11 SDK automatically, so the
# repository builds on machines other than the one it was authored on.
function Find-MsvcToolset {
    $roots = @(
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio",
        "$env:ProgramFiles\Microsoft Visual Studio"
    )
    $found = @()
    foreach ($root in $roots) {
        if (-not (Test-Path $root)) { continue }
        $found += Get-ChildItem $root -Directory -ErrorAction SilentlyContinue | ForEach-Object {
            Get-ChildItem $_.FullName -Directory -ErrorAction SilentlyContinue | ForEach-Object {
                $tools = Join-Path $_.FullName 'VC\Tools\MSVC'
                if (Test-Path $tools) { Get-ChildItem $tools -Directory }
            }
        }
    }
    return $found | Sort-Object Name -Descending | Select-Object -First 1
}

function Find-WindowsSdk {
    $root = "${env:ProgramFiles(x86)}\Windows Kits\10\Include"
    if (-not (Test-Path $root)) { return $null }
    return Get-ChildItem $root -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName 'um') } |
        Sort-Object Name -Descending | Select-Object -First 1
}

$msvc = Find-MsvcToolset
if (-not $msvc) { throw 'MSVC toolchain not found. Install Visual Studio Build Tools with the C++ workload.' }

$sdk = Find-WindowsSdk
if (-not $sdk) { throw 'Windows 10/11 SDK not found.' }

$msvcPath = $msvc.FullName
$sdkPath = "${env:ProgramFiles(x86)}\Windows Kits\10"
$sdkVer = $sdk.Name

$env:INCLUDE = "$msvcPath\include;$sdkPath\Include\$sdkVer\um;$sdkPath\Include\$sdkVer\shared;$sdkPath\Include\$sdkVer\ucrt;$sdkPath\Include\$sdkVer\ucrt\shared"
$env:LIB = "$msvcPath\lib\x64;$sdkPath\Lib\$sdkVer\um\x64;$sdkPath\Lib\$sdkVer\ucrt\x64"
$env:PATH = "$msvcPath\bin\Hostx64\x64;$env:PATH"

Push-Location $PSScriptRoot
try {
    & cl /nologo /LD /EHsc /O2 /MD /DUNICODE /D_UNICODE UnityPlayerProxy.cpp /Fe:UnityPlayer.dll
    if ($LASTEXITCODE -ne 0) { throw "Native build failed with exit code $LASTEXITCODE" }
}
finally { Pop-Location }

Get-Item (Join-Path $PSScriptRoot 'UnityPlayer.dll') | Select-Object FullName, Length
