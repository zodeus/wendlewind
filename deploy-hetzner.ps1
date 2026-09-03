# Provision and deploy Wendlemire.Server to a single Hetzner Cloud VM.
# Reads HCLOUD_TOKEN and CLOUDFLARE_API_TOKEN from the process or user environment.
#
# Usage:
#   .\deploy-hetzner.ps1
#   .\deploy-hetzner.ps1 deploy
#   .\deploy-hetzner.ps1 clients
#   .\deploy-hetzner.ps1 dns
#   .\deploy-hetzner.ps1 snapshot
#   .\deploy-hetzner.ps1 status
#   .\deploy-hetzner.ps1 destroy
#   .\deploy-hetzner.ps1 -Domain wendlemire.com
#   .\deploy-hetzner.ps1 -Location hil -Type cpx11
#
# Player data lives on volume <ServerName>-data (default wendlemire-data),
# mounted at /var/lib/wendlemire. Destroy deletes the VM only.

param(
    [Parameter(Position = 0)]
    [ValidateSet("up", "deploy", "clients", "dns", "snapshot", "status", "destroy")]
    [string]$Action = "up",

    [string]$ServerName = "wendlemire",
    [string]$Location = "fsn1",
    [string]$Type = "cx23",
    [string]$Image = "ubuntu-24.04",
    [string]$Domain = "wendlemire.com",
    [string]$SshKeyName = "wendlemire",
    [string]$SshPublicKeyPath = "",
    [string]$SshIdentityPath = "",
    [string]$ClientDir = "",
    [switch]$Force,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
if ($PSVersionTable.PSVersion -ge [version]"7.4") {
    $PSNativeCommandUseErrorActionPreference = $true
}
$script:CachedServerIPv4 = $null
$ProjectRoot = $PSScriptRoot
$ServerProject = Join-Path $ProjectRoot "Wendlemire.Server\Wendlemire.Server.csproj"
$PublishDir = Join-Path $ProjectRoot "artifacts\hetzner-server"
$RemoteAppDir = "/opt/wendlemire"
$RemoteDataDir = "/var/lib/wendlemire"
$VolumeName = "$ServerName-data"
$VolumeSizeGb = 10
$VolumeMountRoot = "/mnt/wendlemire-data"
$SnapshotKeep = 3
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
    $code = $LASTEXITCODE
    if ($null -eq $code) {
        $code = $(if ($?) { 0 } else { 1 })
    }
    if ($code -ne 0) {
        throw "$File $($Arguments -join ' ') failed with exit $code"
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
    if ($null -eq $code) {
        $code = $(if ($?) { 0 } else { 1 })
    }
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
    if (-not [string]::IsNullOrWhiteSpace($script:CachedServerIPv4)) {
        return $script:CachedServerIPv4
    }

    $server = Get-Server
    if (-not $server) {
        throw "Hetzner server '$ServerName' does not exist. Run .\deploy-hetzner.ps1 first."
    }

    $ip = $server.public_net.ipv4.ip
    if ([string]::IsNullOrWhiteSpace($ip)) {
        throw "Hetzner server '$ServerName' has no IPv4 address."
    }

    $script:CachedServerIPv4 = $ip
    return $ip
}

function Ensure-SshKey {
    $existing = Get-NamedResource -ListArguments @("ssh-key", "list", "-o", "json") -Name $SshKeyName
    if ($existing) {
        Write-Info "Using SSH key '$SshKeyName'."
        return
    }

    $pub = ((Get-Content -LiteralPath $SshPublicKeyPath -Raw) -replace "\s+", " ").Trim()
    $keys = @(Get-HCloudJson -Arguments @("ssh-key", "list", "-o", "json"))
    $same = @($keys) | Where-Object {
        ((($_.public_key -replace "\s+", " ").Trim()) -eq $pub)
    } | Select-Object -First 1
    if ($same) {
        Write-Info "Renaming existing SSH key '$($same.name)' to '$SshKeyName'..."
        Invoke-HCloud @("ssh-key", "update", $same.name, "--name", $SshKeyName)
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
        $previous = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        & ssh @args 2>$null
        $code = $LASTEXITCODE
        $ErrorActionPreference = $previous
        if ($code -eq 0) {
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

function Get-ServerLocation {
    param($Server)

    if ($Server.datacenter.location.name) {
        return $Server.datacenter.location.name
    }
    if ($Server.location.name) {
        return $Server.location.name
    }
    return $Location
}

function Get-Volume {
    return Get-NamedResource -ListArguments @("volume", "list", "-o", "json") -Name $VolumeName
}

function Get-VolumeServerId {
    param($Volume)

    if ($null -eq $Volume -or $null -eq $Volume.server) {
        return $null
    }
    $attached = $Volume.server
    if ($attached -is [ValueType] -or $attached -is [string]) {
        return [int64]$attached
    }
    if ($attached.PSObject.Properties["id"]) {
        return [int64]$attached.id
    }
    return [int64]$attached
}

function Get-VolumeDevice {
    param($Volume)

    if (-not [string]::IsNullOrWhiteSpace($Volume.linux_device)) {
        return $Volume.linux_device
    }
    return "/dev/disk/by-id/scsi-0HC_Volume_$($Volume.id)"
}

function Get-VolumeMountScript {
    param([string]$Device)

    return @"
#!/bin/bash
set -euo pipefail
DEVICE="$Device"
MOUNT_ROOT="$VolumeMountRoot"
DATA_DIR="$RemoteDataDir"
FSTAB=/etc/fstab

echo "Waiting for `$DEVICE ..."
for _ in `$(seq 1 30); do
  if [ -e "`$DEVICE" ]; then
    break
  fi
  udevadm settle || true
  sleep 2
done
if [ ! -e "`$DEVICE" ]; then
  echo "Volume device `$DEVICE did not appear" >&2
  exit 1
fi

fs=""
for _ in `$(seq 1 30); do
  fs=`$(blkid -o value -s TYPE "`$DEVICE" 2>/dev/null || true)
  if [ -n "`$fs" ]; then
    break
  fi
  sleep 2
done
if [ -z "`$fs" ]; then
  echo "Formatting `$DEVICE as ext4..."
  mkfs.ext4 -F -L wendlemire-data "`$DEVICE"
fi

systemctl stop wendlemire || true
mkdir -p "`$MOUNT_ROOT"

# The same device is normally mounted at MOUNT_ROOT and bind-mounted at DATA_DIR.
# Unmount only unexpected extra targets, one path at a time.
while IFS= read -r target; do
  [ -z "`$target" ] && continue
  if [ "`$target" = "`$MOUNT_ROOT" ] || [ "`$target" = "`$DATA_DIR" ]; then
    continue
  fi
  echo "Unmounting `$DEVICE from `$target"
  umount "`$target" || umount -l "`$target"
done < <(findmnt -n -o TARGET --source "`$DEVICE" 2>/dev/null || true)

if ! findmnt -n "`$MOUNT_ROOT" >/dev/null; then
  mount "`$DEVICE" "`$MOUNT_ROOT"
fi

mkdir -p "`$MOUNT_ROOT/data" "`$MOUNT_ROOT/snapshots"

if findmnt -n "`$DATA_DIR" >/dev/null; then
  echo "`$DATA_DIR already mounted"
else
  if [ -d "`$DATA_DIR" ] && [ -n "`$(ls -A "`$DATA_DIR" 2>/dev/null || true)" ]; then
    echo "Migrating existing `$DATA_DIR onto the volume..."
    if [ -z "`$(ls -A "`$MOUNT_ROOT/data" 2>/dev/null || true)" ]; then
      cp -a "`$DATA_DIR/." "`$MOUNT_ROOT/data/"
    fi
    rm -rf "`$DATA_DIR"
  elif [ -d "`$DATA_DIR" ]; then
    rmdir "`$DATA_DIR" 2>/dev/null || rm -rf "`$DATA_DIR"
  fi
  mkdir -p "`$DATA_DIR"
  mount --bind "`$MOUNT_ROOT/data" "`$DATA_DIR"
fi

UUID=`$(blkid -s UUID -o value "`$DEVICE")
if ! grep -q "UUID=`$UUID" "`$FSTAB"; then
  echo "UUID=`$UUID `$MOUNT_ROOT ext4 defaults,nofail 0 2" >> "`$FSTAB"
fi
if ! grep -qF "`$MOUNT_ROOT/data `$DATA_DIR " "`$FSTAB"; then
  echo "`$MOUNT_ROOT/data `$DATA_DIR none bind,nofail 0 0" >> "`$FSTAB"
fi

if id -u wendlemire >/dev/null 2>&1; then
  chown wendlemire:wendlemire "`$MOUNT_ROOT/data"
  chown -R wendlemire:wendlemire "`$DATA_DIR"
fi

echo "Volume mounted at `$MOUNT_ROOT; data at `$DATA_DIR"
findmnt "`$DATA_DIR" || true
"@
}

function Mount-DataVolume {
    param($Volume)

    $device = Get-VolumeDevice $Volume
    $stage = Join-Path $env:TEMP "wendlemire-hetzner-volume"
    New-Item -ItemType Directory -Force -Path $stage | Out-Null
    $scriptPath = Join-Path $stage "mount-volume.sh"
    Write-UnixFile -Path $scriptPath -Content (Get-VolumeMountScript -Device $device)
    Write-Info "Mounting volume '$VolumeName' at $RemoteDataDir..."
    Copy-ToRemote -LocalPath $scriptPath -RemotePath "/tmp/wendlemire-mount-volume.sh"
    Invoke-Remote "bash /tmp/wendlemire-mount-volume.sh"
}

function Ensure-Volume {
    $server = Get-Server
    if (-not $server) {
        throw "Hetzner server '$ServerName' does not exist. Run .\deploy-hetzner.ps1 first."
    }

    $volume = Get-Volume
    if (-not $volume) {
        $serverLocation = Get-ServerLocation $server
        Write-Info "Creating ${VolumeSizeGb}GB volume '$VolumeName' in $serverLocation..."
        Invoke-HCloud @(
            "volume", "create",
            "--name", $VolumeName,
            "--size", "$VolumeSizeGb",
            "--server", $ServerName,
            "--format", "ext4",
            "--enable-protection", "delete",
            "--label", "app=wendlemire",
            "--label", "role=data"
        )
        $volume = Get-Volume
        if (-not $volume) {
            throw "Created volume '$VolumeName' but could not describe it."
        }
    }
    else {
        Write-Info "Using volume '$VolumeName' ($($volume.size) GB)."
        if (-not $volume.protection -or -not $volume.protection.delete) {
            Write-Info "Enabling delete protection on '$VolumeName'..."
            Invoke-HCloud @("volume", "enable-protection", $VolumeName, "delete")
        }

        $attachedId = Get-VolumeServerId $volume
        if ($null -eq $attachedId) {
            Write-Info "Attaching '$VolumeName' to '$ServerName'..."
            Invoke-HCloud @("volume", "attach", "--server", $ServerName, $VolumeName)
            $volume = Get-Volume
        }
        elseif ($attachedId -ne [int64]$server.id) {
            throw "Volume '$VolumeName' is attached to another server (id $attachedId)."
        }
    }

    Wait-Ssh
    Mount-DataVolume -Volume $volume
}

function New-DataSnapshot {
    # Hetzner Cloud has no volume-snapshot API; keep timestamped tarballs on
    # the volume next to the data so they survive VM destroy and rm of the data dir.
    Write-Info "Snapshotting $RemoteDataDir on volume '$VolumeName' (keep $SnapshotKeep)..."
    Invoke-Remote @"
set -euo pipefail
MOUNT_ROOT="$VolumeMountRoot"
DATA_DIR="$RemoteDataDir"
SNAP="`$MOUNT_ROOT/snapshots"
PREFIX="$VolumeName"

if ! findmnt -n "`$DATA_DIR" >/dev/null; then
  echo "Data volume is not mounted at `$DATA_DIR" >&2
  exit 1
fi
mkdir -p "`$SNAP"
if ! ls -A "`$MOUNT_ROOT/data" 2>/dev/null | grep -q .; then
  echo "No data to snapshot."
  exit 0
fi

systemctl stop wendlemire || true
stamp=`$(date -u +%Y%m%d-%H%M%S)
archive="`$SNAP/`$PREFIX-`$stamp.tgz"
tar -C "`$MOUNT_ROOT/data" --exclude=downloads -czf "`$archive" .
systemctl start wendlemire || true

ls -1t "`$SNAP/`$PREFIX"-*.tgz 2>/dev/null | tail -n +$($SnapshotKeep + 1) | while read -r old; do
  rm -f "`$old"
  echo "Removed old snapshot `$old"
done

echo "Wrote `$archive"
ls -lh "`$SNAP"
"@
    Write-Success "Snapshot saved on volume '$VolumeName'."
}

function Get-UserOrProcessEnv {
    param([string]$Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        $value = [Environment]::GetEnvironmentVariable($Name, "User")
    }
    if ([string]::IsNullOrWhiteSpace($value)) {
        $value = [Environment]::GetEnvironmentVariable($Name, "Machine")
    }
    return $value
}

function Get-CloudflareAuthHeaders {
    $token = Get-UserOrProcessEnv "CLOUDFLARE_API_TOKEN"
    if (-not [string]::IsNullOrWhiteSpace($token)) {
        $env:CLOUDFLARE_API_TOKEN = $token
        return @{
            Authorization = "Bearer $token"
        }
    }

    $key = Get-UserOrProcessEnv "CLOUDFLARE_API_KEY"
    $email = Get-UserOrProcessEnv "CLOUDFLARE_EMAIL"
    if (-not [string]::IsNullOrWhiteSpace($key) -and -not [string]::IsNullOrWhiteSpace($email)) {
        $env:CLOUDFLARE_API_KEY = $key
        $env:CLOUDFLARE_EMAIL = $email
        return @{
            "X-Auth-Email" = $email
            "X-Auth-Key"   = $key
        }
    }

    return $null
}

function Get-HetznerIPv6Host {
    param([string]$Raw)

    if ([string]::IsNullOrWhiteSpace($Raw)) {
        return $null
    }

    $addr = ($Raw.Trim() -split "/")[0]
    if ($addr.EndsWith("::")) {
        return $addr + "1"
    }
    return $addr
}

function Get-DnsZoneName {
    $hostName = $Domain.Trim().TrimEnd(".").ToLowerInvariant()
    $parts = $hostName.Split(".")
    if ($parts.Count -lt 2) {
        throw "Domain '$Domain' is not a valid hostname."
    }
    return "$($parts[-2]).$($parts[-1])"
}

function Test-IsApexDomain {
    param([string]$Name)

    return $Name.Trim().TrimEnd(".").ToLowerInvariant() -eq (Get-DnsZoneName)
}

function Get-SiteHostnames {
    $primary = $Domain.Trim().TrimEnd(".").ToLowerInvariant()
    $names = @($primary)
    if (Test-IsApexDomain $primary) {
        $names += "www.$primary"
    }
    return $names
}

function Invoke-Cloudflare {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Method,
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [object]$Body = $null
    )

    $uri = "https://api.cloudflare.com/client/v4$Path"
    $headers = Get-CloudflareAuthHeaders
    $params = @{
        Method      = $Method
        Uri         = $uri
        Headers     = $headers
        ContentType = "application/json"
    }
    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Compress -Depth 6)
    }

    try {
        $response = Invoke-RestMethod @params
    }
    catch {
        $detail = $_.ErrorDetails.Message
        if ([string]::IsNullOrWhiteSpace($detail)) {
            $detail = $_.Exception.Message
        }
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq 401 -or $status -eq 403) {
            throw "Cloudflare rejected the API credentials ($status). Create a token at https://dash.cloudflare.com/profile/api-tokens with Zone DNS Edit for wendlemire.com, then set CLOUDFLARE_API_TOKEN. $detail"
        }
        throw "Cloudflare $Method $Path failed. $detail"
    }

    if (-not $response.success) {
        $errors = ($response.errors | ForEach-Object { "$($_.code): $($_.message)" }) -join "; "
        throw "Cloudflare $Method $Path failed. $errors"
    }

    return $response
}

function Set-CloudflareAddress {
    param(
        [string]$ZoneId,
        [string]$Name,
        [string]$Type,
        [string]$Content
    )

    $query = "type=$Type&name=$([uri]::EscapeDataString($Name))"
    $list = Invoke-Cloudflare -Method GET -Path "/zones/$ZoneId/dns_records?$query"
    $existing = @($list.result) | Select-Object -First 1
    $payload = @{
        type    = $Type
        name    = $Name
        content = $Content
        ttl     = 1
        proxied = $false
    }

    if ($existing) {
        if ($existing.content -eq $Content -and -not $existing.proxied) {
            Write-Info "DNS $Type $Name already points at $Content."
            return
        }

        Write-Info "Updating $Type $Name → $Content..."
        Invoke-Cloudflare -Method PUT -Path "/zones/$ZoneId/dns_records/$($existing.id)" -Body $payload | Out-Null
        return
    }

    Write-Info "Creating $Type $Name → $Content..."
    Invoke-Cloudflare -Method POST -Path "/zones/$ZoneId/dns_records" -Body $payload | Out-Null
}

function Ensure-CloudflareDns {
    if ([string]::IsNullOrWhiteSpace($Domain)) {
        return
    }

    if ($null -eq (Get-CloudflareAuthHeaders)) {
        Write-Host "Cloudflare credentials are not set. Skipping DNS and serving at the server IP. Set CLOUDFLARE_API_TOKEN to point $Domain at this VM." -ForegroundColor Yellow
        $script:Domain = ""
        return
    }

    $server = Get-Server
    if (-not $server) {
        throw "Hetzner server '$ServerName' does not exist. Run .\deploy-hetzner.ps1 first."
    }

    $ipv4 = $server.public_net.ipv4.ip
    $ipv6 = Get-HetznerIPv6Host $server.public_net.ipv6.ip

    $zoneName = Get-DnsZoneName
    Write-Info "Ensuring Cloudflare DNS for $zoneName → $ipv4..."

    $zones = Invoke-Cloudflare -Method GET -Path "/zones?name=$([uri]::EscapeDataString($zoneName))"
    $zone = @($zones.result) | Select-Object -First 1
    if (-not $zone) {
        $accounts = Invoke-Cloudflare -Method GET -Path "/accounts"
        $account = @($accounts.result) | Select-Object -First 1
        if (-not $account) {
            throw "Cloudflare token has no accounts. Create a zone for $zoneName in the Cloudflare dashboard first."
        }

        Write-Info "Creating Cloudflare zone '$zoneName'..."
        $created = Invoke-Cloudflare -Method POST -Path "/zones" -Body @{
            name    = $zoneName
            account = @{ id = $account.id }
            type    = "full"
        }
        $zone = $created.result
    }

    foreach ($name in (Get-SiteHostnames)) {
        Set-CloudflareAddress -ZoneId $zone.id -Name $name -Type A -Content $ipv4
        if ($ipv6) {
            Set-CloudflareAddress -ZoneId $zone.id -Name $name -Type AAAA -Content $ipv6
        }
    }

    $ns = @($zone.name_servers)
    if ($ns.Count -gt 0) {
        Write-Info "Cloudflare nameservers: $($ns -join ', ')"
        if ($zone.status -ne "active") {
            Write-Host "Zone status is '$($zone.status)'. If $zoneName was registered outside Cloudflare, set those nameservers at the registrar." -ForegroundColor Yellow
        }
    }

    Write-Success "Cloudflare DNS points $((Get-SiteHostnames) -join ', ') at $ipv4 (DNS only, so Caddy can issue HTTPS)."
}

function Get-Caddyfile {
    if ($Domain) {
        $hosts = (Get-SiteHostnames) -join " "
        return @"
$hosts {
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
RequiresMountsFor=$RemoteDataDir

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
        "--force",
        "--nologo",
        "-p:PublishSingleFile=false",
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-o", $PublishDir
    )

    $required = @(
        (Join-Path $PublishDir "Wendlemire.Server"),
        (Join-Path $PublishDir "Wendlemire.Simulation.dll")
    )
    foreach ($path in $required) {
        if (-not (Test-Path $path)) {
            throw "Publish succeeded but missing $path"
        }
    }
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
if ! systemctl is-active --quiet wendlemire; then
  echo "wendlemire.service failed to start" >&2
  systemctl status wendlemire --no-pager || true
  journalctl -u wendlemire -n 40 --no-pager || true
  exit 1
fi
systemctl reload caddy || systemctl restart caddy
"@

    Write-Info "Installing on the VM..."
    Copy-ToRemote -LocalPath $extractPath -RemotePath "/tmp/wendlemire-extract.sh"
    Invoke-Remote "bash /tmp/wendlemire-setup.sh"
    Invoke-Remote "bash /tmp/wendlemire-extract.sh"
    Assert-RemoteService

    if ($Domain) {
        $ip = $server.public_net.ipv4.ip
        try {
            $resolved = [System.Net.Dns]::GetHostAddresses($Domain) | ForEach-Object { $_.IPAddressToString }
            if ($resolved -notcontains $ip) {
                Write-Host "DNS for $Domain is $($resolved -join ', '); server is $ip. HTTPS will fail until the new records propagate." -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "Could not resolve $Domain yet. HTTPS will fail until Cloudflare nameservers are live and the A record has propagated." -ForegroundColor Yellow
        }
    }
}

function Assert-RemoteService {
    $status = Invoke-Remote "systemctl is-active wendlemire"
    $line = (@($status) | ForEach-Object { "$_".Trim() } | Where-Object { $_ } | Select-Object -Last 1)
    if ($line -ne "active") {
        throw "wendlemire.service is '$line' after install. Check journalctl -u wendlemire."
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

            $lastError = "health status '$($response.status)'"
        }
        catch {
            $lastError = $_.Exception.Message
        }

        Start-Sleep -Seconds 3
    }
    throw "Health check failed for $url. $lastError"
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
    Write-Host "Location $(Get-ServerLocation $server)"
    Write-Host "IPv4     $($server.public_net.ipv4.ip)"
    Write-Host "URL      $(Get-PublicBaseUrl)"

    $volume = Get-Volume
    if ($volume) {
        $attached = if (Get-VolumeServerId $volume) { "attached" } else { "detached" }
        Write-Host "Volume   $($volume.name) $($volume.size)GB $attached"
    }
    else {
        Write-Host "Volume   $VolumeName (missing)"
    }
    Write-Host ""

    Wait-Ssh
    Invoke-Remote "systemctl is-active wendlemire; systemctl is-active caddy; echo DATA=$RemoteDataDir; findmnt $RemoteDataDir || true; du -sh $RemoteDataDir 2>/dev/null || true; echo SNAPSHOTS=$VolumeMountRoot/snapshots; ls -lh $VolumeMountRoot/snapshots 2>/dev/null || true"
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
        $confirm = Read-Host "This deletes VM '$ServerName' ($($server.public_net.ipv4.ip)) and its local disk. Volume '$VolumeName' and its snapshots are kept. Type '$ServerName' to confirm"
        if ($confirm -ne $ServerName) {
            throw "Destroy cancelled."
        }
    }

    Write-Info "Deleting server '$ServerName'..."
    Invoke-HCloud @("server", "delete", $ServerName)
    Write-Success "Deleted '$ServerName'. Firewall, SSH key, and volume '$VolumeName' were left in place."
    if (Get-Volume) {
        Write-Host "Data is still on volume '$VolumeName'. Run .\deploy-hetzner.ps1 to recreate the VM and reattach it."
    }
}

function Deploy-ClientDownloads {
    if ([string]::IsNullOrWhiteSpace($ClientDir)) {
        if ($Action -eq "clients") {
            throw "Pass -ClientDir pointing at RELEASE\\ with the client zips."
        }
        return
    }

    if (-not (Test-Path $ClientDir)) {
        throw "Client directory not found: $ClientDir"
    }

    $zips = @(Get-ChildItem -Path $ClientDir -Filter "*.zip" -File)
    if ($zips.Count -eq 0) {
        throw "No client zips in $ClientDir. Run publish-release.ps1 first."
    }

    $remoteDownloads = "$RemoteDataDir/downloads"
    Write-Info "Uploading $($zips.Count) client zip(s) to $remoteDownloads ..."
    Invoke-Remote "mkdir -p $remoteDownloads"

    foreach ($zip in $zips) {
        Write-Info "  $($zip.Name)"
        Copy-ToRemote -LocalPath $zip.FullName -RemotePath "/tmp/$($zip.Name)"
        Invoke-Remote "mv /tmp/$($zip.Name) $remoteDownloads/$($zip.Name)"
    }

    $manifest = Join-Path $ClientDir "latest.json"
    if (Test-Path $manifest) {
        Copy-ToRemote -LocalPath $manifest -RemotePath "/tmp/latest.json"
        Invoke-Remote "mv /tmp/latest.json $remoteDownloads/latest.json"
    }

    Invoke-Remote "chown -R wendlemire:wendlemire $remoteDownloads"
    Write-Success "Client downloads are on the server."
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
    Write-Host "Data:    $RemoteDataDir on volume $VolumeName (survives deploys and destroy)"
}

try {
    Get-HCloudToken
    $HCloud = Get-HCloudExe
    if ($Action -ne "dns") {
        Resolve-SshPaths
    }

    switch ($Action) {
        "up" {
            Ensure-SshKey
            Ensure-Firewall
            Ensure-Server
            Ensure-Volume
            Ensure-CloudflareDns
            New-DataSnapshot
            Deploy-App
            Deploy-ClientDownloads
            Wait-Health
            Show-Summary
        }
        "deploy" {
            Ensure-Volume
            Ensure-CloudflareDns
            New-DataSnapshot
            Deploy-App
            Deploy-ClientDownloads
            Wait-Health
            Show-Summary
        }
        "clients" {
            Wait-Ssh
            Deploy-ClientDownloads
            Write-Success "Clients: $(Get-PublicBaseUrl)/download/win-x64"
        }
        "dns" {
            Ensure-CloudflareDns
        }
        "snapshot" {
            Ensure-Volume
            New-DataSnapshot
        }
        "status" {
            Show-Status
        }
        "destroy" {
            Remove-Server
        }
    }
}
catch {
    Write-Host ""
    Write-Host "DEPLOY FAILED: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
