# Provision and deploy Wendlemire.Server to a single Hetzner Cloud VM.
# Reads HCLOUD_TOKEN from the process or user environment (same name as hcloud / Terraform).
#
# Usage:
#   .\deploy-hetzner.ps1
#   .\deploy-hetzner.ps1 deploy
#   .\deploy-hetzner.ps1 status
#   .\deploy-hetzner.ps1 destroy
#   .\deploy-hetzner.ps1 -Domain arena.example.com
#   .\deploy-hetzner.ps1 -Location hil -Type cpx11

param(
    [Parameter(Position = 0)]
    [ValidateSet("up", "deploy", "status", "destroy")]
    [string]$Action = "up",

    [string]$ServerName = "wendlemire",
    [string]$Location = "fsn1",
    [string]$Type = "cx23",
    [string]$Image = "ubuntu-24.04",
    [string]$Domain = "",
    [string]$SshKeyName = "wendlemire",
    [string]$SshPublicKeyPath = "",
    [string]$SshIdentityPath = "",
    [switch]$Force,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$ServerProject = Join-Path $ProjectRoot "Wendlemire.Server\Wendlemire.Server.csproj"
$PublishDir = Join-Path $ProjectRoot "artifacts\hetzner-server"
$RemoteAppDir = "/opt/wendlemire"
$RemoteDataDir = "/var/lib/wendlemire"
$HCloud = $null

function Write-Info { param($Message) Write-Host $Message -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host $Message -ForegroundColor Green }

function Write-UnixFile {
    param(
        [string]$Path,
        [string]$Content
    )

    $normalized = ($Content -replace "`r`n", "`n" -replace "`r", "`n").TrimEnd() + "`n"
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $normalized, $utf8)
}

function Invoke-Native {
    param(
        [Parameter(Mandatory = $true)]
        [string]$File,
        [string[]]$Arguments = @()
    )

    & $File @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$File $($Arguments -join ' ') failed with exit $LASTEXITCODE"
    }
}

function Get-HCloudToken {
    $token = $env:HCLOUD_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) {
        $token = [Environment]::GetEnvironmentVariable("HCLOUD_TOKEN", "User")
    }
    if ([string]::IsNullOrWhiteSpace($token)) {
        $token = [Environment]::GetEnvironmentVariable("HCLOUD_TOKEN", "Machine")
    }
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "HCLOUD_TOKEN is not set. Set a 64-character Hetzner Cloud API token as a user environment variable."
    }

    $env:HCLOUD_TOKEN = $token
}

function Get-HCloudExe {
    $existing = Get-Command hcloud -ErrorAction SilentlyContinue
    if ($existing) {
        return $existing.Source
    }

    $toolsDir = Join-Path $ProjectRoot ".tools"
    $exe = Join-Path $toolsDir "hcloud.exe"
    if (Test-Path $exe) {
        return $exe
    }

    Write-Info "hcloud CLI not on PATH. Downloading a portable copy to .tools\hcloud.exe..."
    New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
    $zip = Join-Path $toolsDir "hcloud.zip"
    $url = "https://github.com/hetznercloud/cli/releases/latest/download/hcloud-windows-amd64.zip"
    Invoke-WebRequest -Uri $url -OutFile $zip
    Expand-Archive -Path $zip -DestinationPath $toolsDir -Force
    Remove-Item $zip -Force

    if (-not (Test-Path $exe)) {
        $found = Get-ChildItem $toolsDir -Recurse -Filter "hcloud.exe" | Select-Object -First 1
        if (-not $found) {
            throw "Downloaded hcloud but could not find hcloud.exe. Install it with: winget install HetznerCloud.CLI"
        }
        Copy-Item $found.FullName $exe -Force
    }

    return $exe
}

function Invoke-HCloud {
    param([string[]]$Arguments)
    Invoke-Native -File $HCloud -Arguments $Arguments
}

function Get-HCloudJson {
    param([string[]]$Arguments)

    $output = & $HCloud @Arguments
    $code = $LASTEXITCODE
    $raw = ($output | Out-String).Trim()
    if ($code -ne 0) {
        throw "hcloud $($Arguments -join ' ') failed with exit $code"
    }
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return $null
    }
    return $raw | ConvertFrom-Json
}

function Resolve-SshPaths {
    $sshDir = Join-Path $env:USERPROFILE ".ssh"
    if (-not $SshPublicKeyPath) {
        foreach ($name in @("id_ed25519.pub", "id_rsa.pub")) {
            $candidate = Join-Path $sshDir $name
            if (Test-Path $candidate) {
                $script:SshPublicKeyPath = $candidate
                break
            }
        }
    }
    if (-not $SshPublicKeyPath -or -not (Test-Path $SshPublicKeyPath)) {
        throw "No SSH public key found. Create one with ssh-keygen or pass -SshPublicKeyPath."
    }

    if (-not $SshIdentityPath) {
        $script:SshIdentityPath = $SshPublicKeyPath -replace "\.pub$", ""
    }
    if (-not (Test-Path $SshIdentityPath)) {
        throw "SSH private key not found at $SshIdentityPath. Pass -SshIdentityPath."
    }
}

function Get-SshArguments {
    param([string[]]$Extra = @())

    return @(
        "-i", $SshIdentityPath,
        "-o", "IdentitiesOnly=yes",
        "-o", "StrictHostKeyChecking=accept-new",
        "-o", "BatchMode=yes"
    ) + $Extra
}

function Invoke-Remote {
    param([string]$Command)

    $unix = ($Command -replace "`r`n", "`n" -replace "`r", "`n").Trim()
    $ip = Get-ServerIPv4
    $args = (Get-SshArguments) + @("root@$ip", $unix)
    Invoke-Native -File "ssh" -Arguments $args
}

function Copy-ToRemote {
    param(
        [string]$LocalPath,
        [string]$RemotePath
    )

    $ip = Get-ServerIPv4
    $args = (Get-SshArguments) + @($LocalPath, "root@${ip}:$RemotePath")
    Invoke-Native -File "scp" -Arguments $args
}

function Get-NamedResource {
    param(
        [string[]]$ListArguments,
        [string]$Name
    )

    $items = Get-HCloudJson -Arguments $ListArguments
    if (-not $items) {
        return $null
    }
    return @($items) | Where-Object { $_.name -eq $Name } | Select-Object -First 1
}

function Get-Server {
    return Get-NamedResource -ListArguments @("server", "list", "-o", "json") -Name $ServerName
}

function Get-ServerIPv4 {
    $server = Get-Server
    if (-not $server) {
        throw "Hetzner server '$ServerName' does not exist. Run .\deploy-hetzner.ps1 first."
    }
    return $server.public_net.ipv4.ip
}

function Ensure-SshKey {
    $existing = Get-NamedResource -ListArguments @("ssh-key", "list", "-o", "json") -Name $SshKeyName
    if ($existing) {
        Write-Info "Using SSH key '$SshKeyName'."
        return
    }

    Write-Info "Uploading SSH public key '$SshKeyName' from $SshPublicKeyPath..."
    Invoke-HCloud @("ssh-key", "create", "--name", $SshKeyName, "--public-key-from-file", $SshPublicKeyPath)
}

function Ensure-Firewall {
    $name = $ServerName
    $existing = Get-NamedResource -ListArguments @("firewall", "list", "-o", "json") -Name $name
    if (-not $existing) {
        Write-Info "Creating firewall '$name' (22/80/443)..."
        Invoke-HCloud @("firewall", "create", "--name", $name)
        foreach ($port in @("22", "80", "443")) {
            Invoke-HCloud @(
                "firewall", "add-rule", $name,
                "--direction", "in",
                "--protocol", "tcp",
                "--port", $port,
                "--source-ips", "0.0.0.0/0",
                "--source-ips", "::/0"
            )
        }
    }
    else {
        Write-Info "Using firewall '$name'."
    }
}

function Wait-Ssh {
    $ip = Get-ServerIPv4
    Write-Info "Waiting for SSH on $ip..."
    $deadline = (Get-Date).AddMinutes(3)
    $args = (Get-SshArguments) + @("-o", "ConnectTimeout=5", "root@$ip", "true")
    while ((Get-Date) -lt $deadline) {
        & ssh @args 2>$null
        if ($LASTEXITCODE -eq 0) {
            return
        }
        Start-Sleep -Seconds 5
    }
    throw "Timed out waiting for SSH on $ip."
}

function Assert-ServerTypeAvailable {
    $types = @(Get-HCloudJson -Arguments @("server-type", "list", "-o", "json"))
    $match = $types | Where-Object { $_.name -eq $Type } | Select-Object -First 1
    if (-not $match) {
        $names = ($types | ForEach-Object { $_.name }) -join ", "
        throw "Unknown server type '$Type'. Available: $names"
    }

    $locations = @(Get-TypeLocations -TypeInfo $match)
    if ($locations.Count -gt 0 -and $locations -notcontains $Location) {
        $here = ($types | Where-Object { (Get-TypeLocations -TypeInfo $_) -contains $Location } |
            ForEach-Object { "$($_.name) ($($_.cores) vCPU, $($_.memory) GB)" }) -join ", "
        throw "Server type '$Type' is not available in '$Location'. Types in ${Location}: $here"
    }
}

function Get-TypeLocations {
    param($TypeInfo)

    return @($TypeInfo.locations | ForEach-Object {
        if ($_ -is [string]) {
            return $_
        }
        if ($null -eq $_.available -or $_.available) {
            return $_.name
        }
    }) | Where-Object { $_ }
}

function Ensure-Server {
    $server = Get-Server
    if ($server) {
        Write-Info "Server '$ServerName' already exists ($($server.public_net.ipv4.ip))."
        if ($server.status -ne "running") {
            Write-Info "Powering on '$ServerName'..."
            Invoke-HCloud @("server", "poweron", $ServerName)
            Wait-Ssh
        }
        return
    }

    Assert-ServerTypeAvailable
    Write-Info "Creating $Type in $Location named '$ServerName' (always-on, billed hourly up to the monthly cap)..."
    Invoke-HCloud @(
        "server", "create",
        "--name", $ServerName,
        "--type", $Type,
        "--image", $Image,
        "--location", $Location,
        "--ssh-key", $SshKeyName,
        "--firewall", $ServerName,
        "--label", "app=wendlemire"
    )
    Wait-Ssh
}

function Get-Caddyfile {
    if ($Domain) {
        return @"
$Domain {
	reverse_proxy 127.0.0.1:5080
}
"@
    }

    return @"
:80 {
	reverse_proxy 127.0.0.1:5080
}
"@
}

function Get-SystemdUnit {
    return @"
[Unit]
Description=Wendlemire Server
After=network.target
Wants=network-online.target

[Service]
Type=simple
User=wendlemire
Group=wendlemire
WorkingDirectory=$RemoteAppDir
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5080
Environment=WENDLEMIRE_DATA=$RemoteDataDir
EnvironmentFile=-/etc/wendlemire.env
ExecStart=$RemoteAppDir/Wendlemire.Server
Restart=always
RestartSec=3
KillSignal=SIGINT

[Install]
WantedBy=multi-user.target
"@
}

function Get-RemoteSetupScript {
    return @"
#!/bin/bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

apt-get update
apt-get install -y caddy curl

if ! id -u wendlemire >/dev/null 2>&1; then
  useradd --system --home $RemoteDataDir --shell /usr/sbin/nologin wendlemire
fi

mkdir -p $RemoteAppDir $RemoteDataDir
chown wendlemire:wendlemire $RemoteAppDir $RemoteDataDir

if [ ! -f /etc/wendlemire.env ]; then
  umask 077
  python3 -c "import secrets; print('WENDLEMIRE_ADMIN_PASSWORD=' + secrets.token_hex(12))" > /etc/wendlemire.env
  chmod 600 /etc/wendlemire.env
fi

install -m 644 /tmp/wendlemire.service /etc/systemd/system/wendlemire.service
install -m 644 /tmp/Caddyfile /etc/caddy/Caddyfile

systemctl daemon-reload
systemctl enable --now caddy
systemctl enable wendlemire
systemctl reload caddy || systemctl restart caddy
"@
}

function Publish-Server {
    if ($SkipBuild -and (Test-Path (Join-Path $PublishDir "Wendlemire.Server"))) {
        Write-Info "Skipping publish; using $PublishDir"
        return
    }

    Write-Info "Publishing Wendlemire.Server (linux-x64, self-contained)..."
    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null

    Invoke-Native -File "dotnet" -Arguments @(
        "publish", $ServerProject,
        "-c", "Release",
        "-r", "linux-x64",
        "--self-contained", "true",
        "--nologo",
        "-p:PublishSingleFile=false",
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-o", $PublishDir
    )
}

function Deploy-App {
    $server = Get-Server
    if (-not $server) {
        throw "Hetzner server '$ServerName' does not exist. Run .\deploy-hetzner.ps1 first."
    }

    Wait-Ssh
    Publish-Server

    $stage = Join-Path $env:TEMP "wendlemire-hetzner"
    if (Test-Path $stage) {
        Remove-Item $stage -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $stage | Out-Null

    $archive = Join-Path $stage "server.tgz"
    $unitPath = Join-Path $stage "wendlemire.service"
    $caddyPath = Join-Path $stage "Caddyfile"
    $setupPath = Join-Path $stage "setup.sh"

    if (Test-Path $archive) {
        Remove-Item $archive -Force
    }
    Invoke-Native -File "tar" -Arguments @("-czf", $archive, "-C", $PublishDir, ".")
    Write-UnixFile -Path $unitPath -Content (Get-SystemdUnit)
    Write-UnixFile -Path $caddyPath -Content (Get-Caddyfile)
    Write-UnixFile -Path $setupPath -Content (Get-RemoteSetupScript)

    Write-Info "Uploading app and host config..."
    Copy-ToRemote -LocalPath $archive -RemotePath "/tmp/wendlemire-server.tgz"
    Copy-ToRemote -LocalPath $unitPath -RemotePath "/tmp/wendlemire.service"
    Copy-ToRemote -LocalPath $caddyPath -RemotePath "/tmp/Caddyfile"
    Copy-ToRemote -LocalPath $setupPath -RemotePath "/tmp/wendlemire-setup.sh"

    $extractPath = Join-Path $stage "extract.sh"
    Write-UnixFile -Path $extractPath -Content @"
#!/bin/bash
set -euo pipefail
rm -rf $RemoteAppDir/*
tar -xzf /tmp/wendlemire-server.tgz -C $RemoteAppDir
chmod +x $RemoteAppDir/Wendlemire.Server
chown -R wendlemire:wendlemire $RemoteAppDir $RemoteDataDir
systemctl restart wendlemire
systemctl reload caddy || systemctl restart caddy
"@

    Write-Info "Installing on the VM..."
    Copy-ToRemote -LocalPath $extractPath -RemotePath "/tmp/wendlemire-extract.sh"
    Invoke-Remote "bash /tmp/wendlemire-setup.sh"
    Invoke-Remote "bash /tmp/wendlemire-extract.sh"

    if ($Domain) {
        $ip = $server.public_net.ipv4.ip
        try {
            $resolved = [System.Net.Dns]::GetHostAddresses($Domain) | ForEach-Object { $_.IPAddressToString }
            if ($resolved -notcontains $ip) {
                Write-Host "DNS for $Domain is $($resolved -join ', '); server is $ip. HTTPS will fail until DNS points here." -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "Could not resolve $Domain yet. Point it at $($server.public_net.ipv4.ip) before Let's Encrypt can issue a cert." -ForegroundColor Yellow
        }
    }
}

function Wait-Health {
    $base = Get-PublicBaseUrl
    $url = "$base/health"
    Write-Info "Checking $url ..."
    $deadline = (Get-Date).AddMinutes(2)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-RestMethod -Uri $url -TimeoutSec 10
            if ($response.status -eq "ok") {
                Write-Success "Healthy. zones=$($response.zones) pawns=$($response.pawns) pool=$($response.pool)"
                return
            }
        }
        catch {
            $lastError = $_
            Start-Sleep -Seconds 3
        }
    }
    throw "Health check failed for $url. $($lastError.Exception.Message)"
}

function Get-PublicBaseUrl {
    if ($Domain) {
        return "https://$Domain"
    }
    return "http://$(Get-ServerIPv4)"
}

function Get-AdminPassword {
    $ip = Get-ServerIPv4
    $args = (Get-SshArguments) + @(
        "root@$ip",
        "sed -n 's/^WENDLEMIRE_ADMIN_PASSWORD=//p' /etc/wendlemire.env"
    )
    $output = & ssh @args
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read WENDLEMIRE_ADMIN_PASSWORD from /etc/wendlemire.env"
    }

    $password = (@($output) | ForEach-Object { "$_".Trim() } | Where-Object { $_ } | Select-Object -Last 1)
    if ([string]::IsNullOrWhiteSpace($password)) {
        throw "WENDLEMIRE_ADMIN_PASSWORD is missing from /etc/wendlemire.env"
    }
    return $password
}

function Show-Status {
    $server = Get-Server
    if (-not $server) {
        Write-Host "No Hetzner server named '$ServerName'."
        return
    }

    Write-Host "Server   $ServerName"
    Write-Host "Status   $($server.status)"
    Write-Host "Type     $($server.server_type.name)"
    Write-Host "Location $($server.datacenter.location.name)"
    Write-Host "IPv4     $($server.public_net.ipv4.ip)"
    Write-Host "URL      $(Get-PublicBaseUrl)"
    Write-Host ""

    Wait-Ssh
    Invoke-Remote "systemctl is-active wendlemire; systemctl is-active caddy; echo DATA=$RemoteDataDir; du -sh $RemoteDataDir 2>/dev/null || true"
    Wait-Health
    Write-Host ""
    Write-Host "Admin:   $(Get-PublicBaseUrl)/admin"
    Write-Host "Admin password: $(Get-AdminPassword)"
    Write-Success "Client: set WENDLEMIRE_SERVER_URL=$(Get-PublicBaseUrl)"
}

function Remove-Server {
    $server = Get-Server
    if (-not $server) {
        Write-Host "No Hetzner server named '$ServerName'."
        return
    }

    if (-not $Force) {
        $confirm = Read-Host "This deletes VM '$ServerName' ($($server.public_net.ipv4.ip)) and its disk. Type '$ServerName' to confirm"
        if ($confirm -ne $ServerName) {
            throw "Destroy cancelled."
        }
    }

    Write-Info "Deleting server '$ServerName'..."
    Invoke-HCloud @("server", "delete", $ServerName)
    Write-Success "Deleted '$ServerName'. Firewall and SSH key were left in place."
}

function Show-Summary {
    $url = Get-PublicBaseUrl
    $adminPassword = Get-AdminPassword
    Write-Host ""
    Write-Success "Wendlemire is up at $url"
    Write-Host "Health:  $url/health"
    Write-Host "Admin:   $url/admin"
    Write-Host "Admin password: $adminPassword"
    Write-Host "Client:  set WENDLEMIRE_SERVER_URL=$url"
    Write-Host "Data:    $RemoteDataDir on the VM (survives deploys, not destroy)"
}

Get-HCloudToken
Resolve-SshPaths
$HCloud = Get-HCloudExe

switch ($Action) {
    "up" {
        Ensure-SshKey
        Ensure-Firewall
        Ensure-Server
        Deploy-App
        Wait-Health
        Show-Summary
    }
    "deploy" {
        Deploy-App
        Wait-Health
        Show-Summary
    }
    "status" {
        Show-Status
    }
    "destroy" {
        Remove-Server
    }
}
