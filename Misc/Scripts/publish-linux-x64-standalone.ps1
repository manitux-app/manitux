param(
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

$RunningOnWindows = if (Get-Variable IsWindows -ErrorAction SilentlyContinue) { $IsWindows } else { $true }

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "../..")

$Project = Join-Path $RepoRoot "Manitux.Desktop/Manitux.Desktop.csproj"
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "builds/linux-x64-standalone"
}

$HelperSource = Join-Path $RepoRoot "Manitux.Desktop/helpers/linux-x64"
$HelperOutput = Join-Path $OutputDir "libs/helpers"
$BuildsRoot = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot "builds"))
$FullOutputDir = [System.IO.Path]::GetFullPath($OutputDir)

Write-Host "Publishing standalone linux-x64 build..."
Write-Host "Output: $OutputDir"

if ((Test-Path $FullOutputDir) -and $FullOutputDir.StartsWith($BuildsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $FullOutputDir -Recurse -Force
}

dotnet publish $Project `
    -c Release `
    -r linux-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:UseSharedCompilation=false `
    -maxcpucount:1 `
    -o $OutputDir

if (-not (Test-Path $HelperSource)) {
    throw "Missing helper source directory: $HelperSource"
}

New-Item -ItemType Directory -Force -Path $HelperOutput | Out-Null
Copy-Item -Path (Join-Path $HelperSource "*") -Destination $HelperOutput -Recurse -Force

if (-not $RunningOnWindows) {
    chmod +x (Join-Path $OutputDir "Manitux.Desktop")

    $tlsClientApi = Join-Path $HelperOutput "tlsclientapi"
    $ytdlp = Join-Path $HelperOutput "ytdlp"

    if (Test-Path $tlsClientApi) {
        chmod +x $tlsClientApi
    }
    if (Test-Path $ytdlp) {
        chmod +x $ytdlp
    }
}

Write-Host "Done."
Write-Host "Run: $(Join-Path $OutputDir 'Manitux.Desktop')"
