# Local ElectricalStore development launcher (Windows).
# Starts API (5080) and Next.js (3000) in separate PowerShell windows.
# Usage (from repo root): .\dev.ps1

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$StateFile = Join-Path $RepoRoot ".dev-processes.json"
$ApiPort = 5080
$FrontendPort = 3000

function Get-ListeningPids {
    param([int]$Port)
    $pids = @()
    try {
        $conns = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
        if ($conns) {
            $pids = @($conns | Select-Object -ExpandProperty OwningProcess -Unique)
        }
    }
    catch {
        $lines = netstat -ano | Select-String -Pattern ":$Port\s+.*LISTENING"
        foreach ($line in $lines) {
            $parts = ($line.ToString() -split "\s+") | Where-Object { $_ -ne "" }
            if ($parts.Length -ge 5) {
                $candidate = 0
                if ([int]::TryParse($parts[-1], [ref]$candidate) -and $candidate -gt 0) {
                    $pids += $candidate
                }
            }
        }
        $pids = @($pids | Select-Object -Unique)
    }
    return $pids
}

function Assert-PortsFree {
    $blocked = @()
    foreach ($item in @(
            @{ Port = $ApiPort; Label = "ASP.NET API" },
            @{ Port = $FrontendPort; Label = "Next.js frontend" }
        )) {
        $pids = Get-ListeningPids -Port $item.Port
        if ($pids.Count -gt 0) {
            $blocked += [pscustomobject]@{ Port = $item.Port; Label = $item.Label; Pids = $pids }
        }
    }

    if ($blocked.Count -eq 0) {
        return
    }

    Write-Host ""
    Write-Host "ERROR: Required development port(s) already in use." -ForegroundColor Red
    foreach ($b in $blocked) {
        Write-Host ("  Port {0} ({1}):" -f $b.Port, $b.Label) -ForegroundColor Yellow
        foreach ($procId in $b.Pids) {
            $name = "(unknown)"
            try {
                $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
                if ($proc) { $name = $proc.ProcessName }
            }
            catch { }
            Write-Host ("    PID {0} - {1}" -f $procId, $name) -ForegroundColor Yellow
        }
    }
    Write-Host ""
    Write-Host "Stop those processes (or run .\stop-dev.ps1 if started by this launcher), then retry." -ForegroundColor Yellow
    Write-Host ("This script will not kill arbitrary processes or move Next.js off port {0}." -f $FrontendPort) -ForegroundColor Yellow
    exit 1
}

Set-Location $RepoRoot

Write-Host "ElectricalStore local launcher" -ForegroundColor Cyan
Write-Host ("Repo: {0}" -f $RepoRoot)
Write-Host ""

Assert-PortsFree

$apiCommand = "Set-Location -LiteralPath '$RepoRoot'; Write-Host 'ElectricalStore API - http://localhost:$ApiPort' -ForegroundColor Cyan; dotnet run --project src/ElectricalStore.Api --launch-profile http"
$frontendDir = Join-Path $RepoRoot "frontend"
$frontendCommand = "Set-Location -LiteralPath '$frontendDir'; Write-Host 'ElectricalStore frontend - http://localhost:$FrontendPort' -ForegroundColor Cyan; npm.cmd run dev"

$apiProc = Start-Process -FilePath "powershell.exe" -WorkingDirectory $RepoRoot -PassThru -ArgumentList @(
    "-NoExit",
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-Command", $apiCommand
)

$frontendProc = Start-Process -FilePath "powershell.exe" -WorkingDirectory $frontendDir -PassThru -ArgumentList @(
    "-NoExit",
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-Command", $frontendCommand
)

$state = [ordered]@{
    startedAtUtc     = (Get-Date).ToUniversalTime().ToString("o")
    apiPort          = $ApiPort
    frontendPort     = $FrontendPort
    apiShellPid      = $apiProc.Id
    frontendShellPid = $frontendProc.Id
}
$state | ConvertTo-Json | Set-Content -LiteralPath $StateFile -Encoding utf8

Write-Host "Started in separate windows:" -ForegroundColor Green
Write-Host ("  API shell PID:      {0}" -f $apiProc.Id)
Write-Host ("  Frontend shell PID: {0}" -f $frontendProc.Id)
Write-Host "  State file:         .dev-processes.json (gitignored)"
Write-Host ""
Write-Host ("Backend:  http://localhost:{0}" -f $ApiPort)
Write-Host ("Health:   http://localhost:{0}/health" -f $ApiPort)
Write-Host ("Frontend: http://localhost:{0}" -f $FrontendPort)
Write-Host ("Swagger:  http://localhost:{0}/swagger" -f $ApiPort)
Write-Host ""
Write-Host "Stop with: .\stop-dev.ps1"
