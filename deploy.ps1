# Deploy the Hetzner server and upload the latest desktop clients to that host.
#
# Usage:
#   .\deploy.ps1
#   .\deploy.ps1 -Version 0.1
#   .\deploy.ps1 -Action deploy -Platform windows
#   .\deploy.ps1 -SkipClients
#   .\deploy.ps1 -SkipServer -Version 0.1
#   .\deploy.ps1 -Domain wendlemire.com
#
# Server work is delegated to deploy-hetzner.ps1.
# Client zips are built by publish-release.ps1 and copied to /var/lib/wendlemire/downloads.

param(
    [ValidateSet("up", "deploy")]
    [string]$Action = "up",

    [string]$Version = "0.1a",

    [ValidateSet("all", "windows", "mac", "current")]
    [string]$Platform = "all",

    [string]$ServerName = "wendlemire",
    [string]$Location = "fsn1",
    [string]$Type = "cx23",
    [string]$Domain = "wendlemire.com",
    [string]$ServerUrl = "",
    [string]$SshKeyName = "wendlemire",
    [string]$SshPublicKeyPath = "",
    [string]$SshIdentityPath = "",

    [switch]$SkipServer,
    [switch]$SkipClients,
    [switch]$SkipBuild,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$Hetzner = Join-Path $ProjectRoot "deploy-hetzner.ps1"
$Release = Join-Path $ProjectRoot "publish-release.ps1"
$ReleaseDir = Join-Path $ProjectRoot "RELEASE"

function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }

function Resolve-ServerUrl {
    if (-not [string]::IsNullOrWhiteSpace($ServerUrl)) {
        return $ServerUrl.Trim().TrimEnd("/")
    }

    if (-not [string]::IsNullOrWhiteSpace($Domain)) {
        return "https://$Domain"
    }

    return "https://wendlemire.com"
}

function Get-HetznerArgs {
    param([string]$HetznerAction)

    $args = @{
        Action     = $HetznerAction
        ServerName = $ServerName
        Location   = $Location
        Type       = $Type
        SshKeyName = $SshKeyName
    }
    if ($Domain) { $args.Domain = $Domain }
    if ($SshPublicKeyPath) { $args.SshPublicKeyPath = $SshPublicKeyPath }
    if ($SshIdentityPath) { $args.SshIdentityPath = $SshIdentityPath }
    if ($SkipBuild) { $args.SkipBuild = $true }
    if ($Force) { $args.Force = $true }
    if (-not $SkipClients) { $args.ClientDir = $ReleaseDir }
    return $args
}

if (-not (Test-Path $Hetzner)) {
    throw "Missing $Hetzner"
}
if (-not (Test-Path $Release)) {
    throw "Missing $Release"
}
if ($SkipServer -and $SkipClients) {
    throw "Nothing to do: both -SkipServer and -SkipClients were passed."
}

$resolvedUrl = Resolve-ServerUrl

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Wendlemire deploy" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

if (-not $SkipClients) {
    Write-Host ""
    Write-Info "Building client zips (v$Version → $resolvedUrl)..."
    & $Release -Version $Version -Platform $Platform -ServerUrl $resolvedUrl
}

if (-not $SkipServer) {
    Write-Host ""
    Write-Info "Deploying server ($Action)..."
    $hetznerArgs = Get-HetznerArgs -HetznerAction $Action
    & $Hetzner @hetznerArgs
}
elseif (-not $SkipClients) {
    Write-Host ""
    Write-Info "Uploading client zips to the server..."
    $hetznerArgs = Get-HetznerArgs -HetznerAction "clients"
    & $Hetzner @hetznerArgs
}

Write-Host ""
Write-Success "Deploy finished."
Write-Host "Server:  $resolvedUrl"
Write-Host "Health:  $resolvedUrl/health"
Write-Host "Admin:   $resolvedUrl/admin"
if (-not $SkipClients) {
    Write-Host "Windows: $resolvedUrl/download/win-x64"
}
Write-Host ""
