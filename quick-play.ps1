# Kill any running Wendlewind server/client and start a fresh pair.
# Usage: .\quick-play.ps1

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot
$ServerProject = Join-Path $Root "Wendlewind.Server\Wendlewind.Server.csproj"
$ClientProject = Join-Path $Root "Wendlewind\Wendlewind.Client.csproj"
$HealthUrl = "http://localhost:5080/health"

function Stop-OwnedProcesses {
    param([int[]]$ProcessIds)

    foreach ($processId in ($ProcessIds | Where-Object { $_ -gt 0 } | Select-Object -Unique)) {
        if ($processId -eq $PID) {
            continue
        }

        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
}

function Stop-WendlewindPair {
    Stop-OwnedProcesses @(
        (Get-Process -Name "Wendlewind", "Wendlewind.Server" -ErrorAction SilentlyContinue).Id
    )

    $dotnetHosts = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" |
        Where-Object {
            $_.CommandLine -and (
                $_.CommandLine -match "Wendlewind\.Server" -or
                $_.CommandLine -match "Wendlewind\.Client" -or
                $_.CommandLine -match "Wendlewind\.dll"
            )
        }
    Stop-OwnedProcesses @($dotnetHosts.ProcessId)

    foreach ($port in 5080, 5088) {
        $listeners = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty OwningProcess -Unique
        Stop-OwnedProcesses @($listeners)
    }

    Start-Sleep -Milliseconds 400
}

function Wait-ServerHealthy {
    $deadline = (Get-Date).AddSeconds(90)
    do {
        try {
            $response = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
        }

        Start-Sleep -Milliseconds 400
    } while ((Get-Date) -lt $deadline)

    throw "Server did not become healthy at $HealthUrl"
}

Write-Host "Stopping existing Wendlewind server/client..."
Stop-WendlewindPair

Write-Host "Starting server..."
$server = Start-Process -FilePath "dotnet" -WorkingDirectory $Root -PassThru -ArgumentList @(
    "run",
    "--project",
    $ServerProject
)

Write-Host "Waiting for $HealthUrl ..."
Wait-ServerHealthy

Write-Host "Starting client..."
try {
    dotnet run --project $ClientProject
}
finally {
    if ($server -and -not $server.HasExited) {
        Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
    }

    Stop-WendlewindPair
}
