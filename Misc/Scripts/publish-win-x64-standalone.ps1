param(
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "../..")

$Project = Join-Path $RepoRoot "Manitux.Desktop/Manitux.Desktop.csproj"
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "builds/win-x64-standalone"
}

$HelperSource = Join-Path $RepoRoot "Manitux.Desktop/helpers/win-x64"
$HelperOutput = Join-Path $OutputDir "libs/helpers"
$BuildsRoot = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot "builds"))
$FullOutputDir = [System.IO.Path]::GetFullPath($OutputDir)

Write-Host "Publishing standalone win-x64 build..."
Write-Host "Output: $OutputDir"

if ((Test-Path $FullOutputDir) -and $FullOutputDir.StartsWith($BuildsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $FullOutputDir -Recurse -Force
}

dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:UseSharedCompilation=false `
    -maxcpucount:1 `
    -o $OutputDir

if (Test-Path $HelperSource) {
    New-Item -ItemType Directory -Force -Path $HelperOutput | Out-Null
    Copy-Item -Path (Join-Path $HelperSource "*") -Destination $HelperOutput -Recurse -Force
} else {
    Write-Host "No Windows helper source directory found: $HelperSource"
}

Write-Host "Done."
Write-Host "Run: $(Join-Path $OutputDir 'Manitux.Desktop.exe')"
