# Deploy the Hetzner server and publish the latest desktop clients to GitHub Releases.
#
# Usage:
#   .\deploy.ps1
#   .\deploy.ps1 -Version 0.1
#   .\deploy.ps1 -Action deploy -Platform windows
#   .\deploy.ps1 -SkipClients
#   .\deploy.ps1 -SkipServer -Version 0.1
#
# Server work is delegated to deploy-hetzner.ps1.
# Client zips and GitHub upload are delegated to publish-release.ps1.

param(
    [ValidateSet("up", "deploy")]
    [string]$Action = "up",

    [string]$Version = "0.1",

    [ValidateSet("all", "windows", "mac", "current")]
    [string]$Platform = "all",

    [string]$ServerName = "wendlewind",
    [string]$Location = "fsn1",
    [string]$Type = "cx23",
    [string]$Domain = "",
    [string]$ServerUrl = "",
    [string]$SshKeyName = "wendlewind",
    [string]$SshPublicKeyPath = "",
    [string]$SshIdentityPath = "",

    [switch]$SkipServer,
    [switch]$SkipClients,
    [switch]$SkipUpload,
    [switch]$SkipBuild,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$Hetzner = Join-Path $ProjectRoot "deploy-hetzner.ps1"
$Release = Join-Path $ProjectRoot "publish-release.ps1"

function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }

function Resolve-ServerUrl {
    if (-not [string]::IsNullOrWhiteSpace($ServerUrl)) {
        return $ServerUrl.Trim().TrimEnd("/")
    }

    if (-not [string]::IsNullOrWhiteSpace($Domain)) {
        return "https://$Domain"
    }

    if (Get-Command hcloud -ErrorAction SilentlyContinue) {
        $previousEap = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        $json = & hcloud server describe $ServerName -o json 2>$null
        $ok = $LASTEXITCODE -eq 0
        $ErrorActionPreference = $previousEap
        if ($ok -and -not [string]::IsNullOrWhiteSpace($json)) {
            $ip = ($json | ConvertFrom-Json).public_net.ipv4.ip
            if (-not [string]::IsNullOrWhiteSpace($ip)) {
                return "http://$ip"
            }
        }
    }

    return "http://5.78.232.9"
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

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Wendlewind deploy" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

if (-not $SkipServer) {
    Write-Host ""
    Write-Info "1/2  Server ($Action)..."
    $hetznerArgs = @{
        Action     = $Action
        ServerName = $ServerName
        Location   = $Location
        Type       = $Type
        SshKeyName = $SshKeyName
    }
    if ($Domain) { $hetznerArgs.Domain = $Domain }
    if ($SshPublicKeyPath) { $hetznerArgs.SshPublicKeyPath = $SshPublicKeyPath }
    if ($SshIdentityPath) { $hetznerArgs.SshIdentityPath = $SshIdentityPath }
    if ($SkipBuild) { $hetznerArgs.SkipBuild = $true }
    if ($Force) { $hetznerArgs.Force = $true }

    & $Hetzner @hetznerArgs
}

$resolvedUrl = Resolve-ServerUrl

if (-not $SkipClients) {
    Write-Host ""
    Write-Info "2/2  Clients (v$Version → $resolvedUrl)..."
    $releaseArgs = @{
        Version   = $Version
        Platform  = $Platform
        ServerUrl = $resolvedUrl
    }
    if ($SkipUpload) { $releaseArgs.SkipUpload = $true }

    & $Release @releaseArgs
}

Write-Host ""
Write-Success "Deploy finished."
Write-Host "Server:  $resolvedUrl"
Write-Host "Health:  $resolvedUrl/health"
Write-Host "Admin:   $resolvedUrl/admin"
if (-not $SkipClients -and -not $SkipUpload) {
    Write-Host "Clients: https://github.com/zodeus/wendlewind/releases/tag/v$Version"
}
Write-Host ""
