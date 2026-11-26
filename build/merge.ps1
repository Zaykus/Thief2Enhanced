param(
    [string]$GameRoot = '',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# Merges the Harmony/MonoMod runtime libraries into Thief2Enhanced.dll so the
# released package only needs the proxy and one managed DLL.
$modRoot = Split-Path -Parent $PSScriptRoot
if (-not $GameRoot) { $GameRoot = Split-Path -Parent $modRoot }

$exe = Join-Path $GameRoot 'Thief Simulator 2.exe'
if (-not (Test-Path $exe)) {
    throw "Thief Simulator 2.exe was not found in '$GameRoot'. Pass -GameRoot '<game folder>'."
}

$project = Join-Path $modRoot 'src\ThiefSimulator2Mod\ThiefSimulator2Mod.csproj'
$built = Join-Path $modRoot "src\ThiefSimulator2Mod\bin\$Configuration\Thief2Enhanced.dll"
$libDir = Join-Path $modRoot 'lib'
$managedDir = Join-Path $GameRoot 'Thief Simulator 2_Data\Managed'

if (-not (Test-Path (Join-Path $managedDir 'Assembly-CSharp.dll'))) {
    throw "Assembly-CSharp.dll was not found in '$managedDir'. Pass -GameRoot '<game folder>'."
}

Write-Host '== Building Thief2Enhanced =='
& dotnet build $project -c $Configuration -v minimal "-p:GameRoot=$GameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Managed build failed.' }

$ilrepack = Get-Command ilrepack -ErrorAction SilentlyContinue
if (-not $ilrepack) {
    $toolFallback = Join-Path $env:USERPROFILE '.dotnet\tools\ilrepack.exe'
    if (Test-Path -LiteralPath $toolFallback) { $ilrepack = $toolFallback }
}
if (-not $ilrepack) {
    throw 'ILRepack was not found. Install it with: dotnet tool install --global dotnet-ilrepack'
}

$refCandidates = @(
    'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8.1',
    'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8',
    'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2',
    'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1',
    'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0'
)
$refDir = $refCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $refDir) { throw 'No .NET Framework reference assemblies were found; install the targeting pack for .NET Framework 4.x.' }

$outDir = Join-Path $modRoot 'build\merged'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$merged = Join-Path $outDir 'Thief2Enhanced.dll'

Write-Host '== Merging runtime libraries =='
& $ilrepack "/out:$merged" "/targetplatform:v4,$refDir" "/lib:$managedDir" "/lib:$libDir" /internalize /ndebug $built `
    (Join-Path $libDir '0Harmony.dll') `
    (Join-Path $libDir 'Mono.Cecil.dll') `
    (Join-Path $libDir 'MonoMod.Utils.dll') `
    (Join-Path $libDir 'MonoMod.RuntimeDetour.dll')
if ($LASTEXITCODE -ne 0) { throw 'ILRepack merge failed.' }

Copy-Item -LiteralPath $merged -Destination $built -Force
Write-Host "Merged DLL: $built"
