param(
    [string]$OutputDir,
    [string]$ZipPath
)

$ErrorActionPreference = "Stop"

$RunningOnWindows = if (Get-Variable IsWindows -ErrorAction SilentlyContinue) { $IsWindows } else { $true }

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "../..")

$Project = Join-Path $RepoRoot "Manitux.Desktop/Manitux.Desktop.csproj"
$RuntimeId = "osx-x64"
$Version = ((dotnet msbuild $Project -getProperty:Version -nologo) | Select-Object -Last 1).Trim()
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = "0.0.0"
}

$AssetName = "Manitux_${RuntimeId}_v$Version"
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "builds/$AssetName"
}
if ([string]::IsNullOrWhiteSpace($ZipPath)) {
    $ZipPath = Join-Path $RepoRoot "builds/$AssetName.zip"
}

$HelperSource = Join-Path $RepoRoot "Manitux.Desktop/helpers/$RuntimeId"
$HelperOutput = Join-Path $OutputDir "libs/helpers"
$BuildsRoot = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot "builds"))
$FullOutputDir = [System.IO.Path]::GetFullPath($OutputDir)
$FullZipPath = [System.IO.Path]::GetFullPath($ZipPath)

Write-Host "Publishing standalone $RuntimeId build..."
Write-Host "Output: $FullOutputDir"
Write-Host "Asset: $FullZipPath"

if ((Test-Path $FullOutputDir) -and $FullOutputDir.StartsWith($BuildsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Remove-Item -LiteralPath $FullOutputDir -Recurse -Force
}

dotnet publish $Project `
    -c Release `
    -r $RuntimeId `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:UseSharedCompilation=false `
    -maxcpucount:1 `
    -o $FullOutputDir

if (Test-Path $HelperSource) {
    New-Item -ItemType Directory -Force -Path $HelperOutput | Out-Null
    Copy-Item -Path (Join-Path $HelperSource "*") -Destination $HelperOutput -Recurse -Force

    if (-not $RunningOnWindows) {
        $tlsClientApi = Join-Path $HelperOutput "tlsclientapi"
        $ytdlp = Join-Path $HelperOutput "ytdlp"

        if (Test-Path $tlsClientApi) {
            chmod +x $tlsClientApi
        }
        if (Test-Path $ytdlp) {
            chmod +x $ytdlp
        }
    }
} else {
    Write-Host "No macOS helper source directory found: $HelperSource"
}

if (-not $RunningOnWindows) {
    python3 (Join-Path $ScriptDir "patch-macho-rpaths.py") (Join-Path $FullOutputDir "libs")
    chmod +x (Join-Path $FullOutputDir "Manitux.Desktop")
}

$ZipParent = Split-Path -Parent $FullZipPath
New-Item -ItemType Directory -Force -Path $ZipParent | Out-Null
if (Test-Path $FullZipPath) {
    Remove-Item -LiteralPath $FullZipPath -Force
}
Compress-Archive -Path (Join-Path $FullOutputDir "*") -DestinationPath $FullZipPath -Force

Write-Host "Done."
Write-Host "Run: $(Join-Path $FullOutputDir 'Manitux.Desktop')"
Write-Host "Release asset: $FullZipPath"
