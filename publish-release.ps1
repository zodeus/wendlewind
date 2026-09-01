# Publish Wendlemire Windows/macOS builds and upload them to GitHub Releases.
# Usage:
#   .\publish-release.ps1
#   .\publish-release.ps1 -Version 0.1 -Platform all
#   .\publish-release.ps1 -Version 0.1 -Platform windows -SkipUpload

param(
    [string]$Version = "0.1",
    [ValidateSet("all", "windows", "mac", "current")]
    [string]$Platform = "all",
    [string]$ServerUrl = "http://5.78.232.9",
    [switch]$SkipUpload
)

$ErrorActionPreference = "Stop"

$ProjectRoot = $PSScriptRoot
$Project = Join-Path $ProjectRoot "Wendlemire\Wendlemire.Client.csproj"
$ReleaseDir = Join-Path $ProjectRoot "RELEASE"
$Tag = "v$Version"

function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }

function Get-PublishTargets {
    param([string]$Requested)

    $targets = @()
    $includeWindows = $false
    $includeMac = $false

    switch ($Requested) {
        "windows" { $includeWindows = $true }
        "mac" { $includeMac = $true }
        "all" { $includeWindows = $true; $includeMac = $true }
        "current" {
            if ($env:OS -eq "Windows_NT") { $includeWindows = $true }
            elseif ($IsMacOS) { $includeMac = $true }
            else { throw "Current OS is not Windows or macOS. Use -Platform windows, mac, or all." }
        }
    }

    if ($includeWindows) {
        $targets += [pscustomobject]@{ Rid = "win-x64"; Label = "Windows x64" }
    }
    if ($includeMac) {
        $targets += [pscustomobject]@{ Rid = "osx-arm64"; Label = "macOS Apple Silicon" }
        $targets += [pscustomobject]@{ Rid = "osx-x64"; Label = "macOS Intel" }
    }

    return $targets
}

function Write-Readme {
    param([string]$PublishDir, [string]$Rid)

    if ($Rid -like "win-*") {
        $body = @"
Wendlemire $Version for Windows

Double-click Wendlemire.exe to play.

The client is already pointed at $ServerUrl. You can change the Server field in the main menu.

If Windows SmartScreen warns about an unknown app, choose More info > Run anyway.
"@
    }
    else {
        $body = @"
Wendlemire $Version for macOS

From Terminal, in this folder:

  chmod +x Wendlemire
  ./Wendlemire

The client is already pointed at $ServerUrl. You can change the Server field in the main menu.

macOS may block unsigned apps. Allow it under System Settings > Privacy & Security, or run:

  xattr -cr .
  chmod +x Wendlemire
  ./Wendlemire
"@
    }

    Set-Content -Path (Join-Path $PublishDir "README.txt") -Value $body.Trim() -Encoding utf8
}

function Write-ClientSettings {
    param([string]$PublishDir)

    $hostUrl = $ServerUrl.Trim().TrimEnd("/")
    $json = "{`"ServerHost`":`"$hostUrl`"}"
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText((Join-Path $PublishDir "client.json"), $json, $utf8)
}

function Compress-ReleaseFolder {
    param([string]$FolderPath, [string]$ZipPath)

    if (Test-Path $ZipPath) {
        Remove-Item $ZipPath -Force
    }

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $FolderPath,
        $ZipPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $true)
}

function Publish-Target {
    param($Target)

    $publishDir = Join-Path $ReleaseDir "build\$($Target.Rid)\Wendlemire"
    $zipName = "Wendlemire-$Version-$($Target.Rid).zip"
    $zipPath = Join-Path $ReleaseDir $zipName

    Write-Host ""
    Write-Info "Publishing $($Target.Label) ($($Target.Rid))..."

    if (Test-Path (Join-Path $ReleaseDir "build\$($Target.Rid)")) {
        Remove-Item (Join-Path $ReleaseDir "build\$($Target.Rid)") -Recurse -Force
    }

    & dotnet publish $Project `
        -c Release `
        -r $Target.Rid `
        --self-contained true `
        --nologo `
        -p:PublishSingleFile=false `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -o $publishDir | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $($Target.Rid)"
    }

    Get-ChildItem $publishDir -Filter "*.pdb" -Recurse -ErrorAction SilentlyContinue | Remove-Item -Force
    Write-Readme -PublishDir $publishDir -Rid $Target.Rid
    Write-ClientSettings -PublishDir $publishDir
    Compress-ReleaseFolder -FolderPath $publishDir -ZipPath $zipPath

    Write-Success "Created $zipName"
    Write-Output $zipPath
}

function Publish-GitHubRelease {
    param([string[]]$ZipPaths)

    Write-Host ""
    Write-Info "Publishing GitHub release ${Tag}..."

    $notesFile = Join-Path $ReleaseDir "release-notes.md"
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($notesFile, @"
Wendlemire $Version

Self-contained game builds. Unzip and run Wendlemire.exe on Windows, or ./Wendlemire on macOS.

"@, $utf8)

    $previousEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    & gh release view $Tag --json tagName | Out-Null
    $releaseExists = ($LASTEXITCODE -eq 0)
    $ErrorActionPreference = $previousEap

    $assetArgs = @($ZipPaths | Where-Object { $_ -like "*.zip" -and (Test-Path $_) })
    if ($assetArgs.Count -eq 0) {
        throw "No zip artifacts were found to upload."
    }

    if ($releaseExists) {
        $ghArgs = @("release", "upload", $Tag, "--clobber") + $assetArgs
        & gh @ghArgs
        if ($LASTEXITCODE -ne 0) { throw "Failed to upload assets to ${Tag}" }
        Write-Success "Updated existing release ${Tag}"
    }
    else {
        $ghArgs = @(
            "release", "create", $Tag,
            "--title", "Wendlemire $Version",
            "--notes-file", $notesFile,
            "--latest"
        ) + $assetArgs
        & gh @ghArgs
        if ($LASTEXITCODE -ne 0) { throw "Failed to create release ${Tag}" }
        Write-Success "Created GitHub release ${Tag}"
    }
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet is required on PATH."
}
if (-not $SkipUpload -and -not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) is required on PATH, or pass -SkipUpload."
}
if (-not (Test-Path $Project)) {
    throw "Client project not found: $Project"
}

New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Wendlemire $Version release" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

$targets = @(Get-PublishTargets -Requested $Platform)
$zips = @()
foreach ($target in $targets) {
    $zips += Publish-Target -Target $target
}

if (-not $SkipUpload) {
    Publish-GitHubRelease -ZipPaths $zips
    Write-Host ""
    Write-Success "Release ${Tag}: https://github.com/zodeus/wendlemire/releases/tag/${Tag}"
}
else {
    Write-Host ""
    Write-Success "Skipped GitHub upload. Artifacts are in $ReleaseDir"
}

Write-Host ""
