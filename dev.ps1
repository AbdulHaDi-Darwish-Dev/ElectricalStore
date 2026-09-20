# Local ElectricalStore development launcher (Windows).
# Official ports: API http://localhost:5180 , Frontend http://localhost:3100
# Usage (from repo root): .\dev.ps1
# Re-running .\dev.ps1 stops launcher-owned previous shells first, then starts fresh.

$ErrorActionPreference = "Stop"
$RepoRoot = $PSScriptRoot
$StateFile = Join-Path $RepoRoot ".dev-processes.json"
$ApiPort = 5180
$FrontendPort = 3100

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
    return @($pids | Where-Object { $_ -gt 0 })
}

function Get-ProcessNameSafe {
    param([int]$ProcessId)
    try {
        $proc = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
        if ($proc) { return $proc.ProcessName }
    }
    catch { }
    return "(unknown)"
}

function Stop-TrackedTree {
    param(
        [Parameter(Mandatory = $true)][int]$ProcessId,
        [string]$Label
    )

    if ($ProcessId -le 0) {
        Write-Host ("  Skip {0} - invalid PID." -f $Label) -ForegroundColor DarkYellow
        return
    }

    $proc = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $proc) {
        Write-Host ("  {0} PID {1} - already stopped." -f $Label, $ProcessId) -ForegroundColor DarkGray
        return
    }

    Write-Host ("  Stopping {0} PID {1} ({2}) and child processes..." -f $Label, $ProcessId, $proc.ProcessName) -ForegroundColor Yellow
    & taskkill.exe /PID $ProcessId /T /F 2>$null | Out-Null
    Start-Sleep -Milliseconds 500

    $still = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($still) {
        Write-Host ("  WARNING: {0} PID {1} still running." -f $Label, $ProcessId) -ForegroundColor Red
    }
    else {
        Write-Host ("  {0} stopped." -f $Label) -ForegroundColor Green
    }
}

function Stop-LauncherOwnedPreviousRun {
    if (-not (Test-Path -LiteralPath $StateFile)) {
        return
    }

    Write-Host "Found .dev-processes.json - stopping previous launcher-owned processes..." -ForegroundColor Cyan

    try {
        $state = Get-Content -LiteralPath $StateFile -Raw -Encoding utf8 | ConvertFrom-Json
    }
    catch {
        Write-Host ("  Stale/unreadable state file - removing: {0}" -f $_) -ForegroundColor DarkYellow
        Remove-Item -LiteralPath $StateFile -Force -ErrorAction SilentlyContinue
        return
    }

    $apiShellPid = 0
    $frontendShellPid = 0
    if ($null -ne $state.apiShellPid) {
        [void][int]::TryParse([string]$state.apiShellPid, [ref]$apiShellPid)
    }
    if ($null -ne $state.frontendShellPid) {
        [void][int]::TryParse([string]$state.frontendShellPid, [ref]$frontendShellPid)
    }

    Stop-TrackedTree -ProcessId $apiShellPid -Label "API shell"
    Stop-TrackedTree -ProcessId $frontendShellPid -Label "Frontend shell"

    Remove-Item -LiteralPath $StateFile -Force -ErrorAction SilentlyContinue
    Write-Host "  Cleared .dev-processes.json" -ForegroundColor Green
    Start-Sleep -Milliseconds 700
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
    Write-Host "ERROR: Required development port(s) occupied by a process that is NOT launcher-owned." -ForegroundColor Red
    Write-Host "This script will NOT kill unrelated processes." -ForegroundColor Red
    foreach ($b in $blocked) {
        Write-Host ("  Port {0} ({1}):" -f $b.Port, $b.Label) -ForegroundColor Yellow
        foreach ($procId in $b.Pids) {
            Write-Host ("    PID {0} - {1}" -f $procId, (Get-ProcessNameSafe -ProcessId $procId)) -ForegroundColor Yellow
        }
    }
    Write-Host ""
    Write-Host "If Visual Studio is hosting ElectricalStore.Api, stop that debug session first." -ForegroundColor Yellow
    Write-Host "Use ONE backend owner at a time: .\dev.ps1 OR Visual Studio (not both)." -ForegroundColor Yellow
    Write-Host ("Do not expect fallback to another port - {0}/{1} must stay fixed." -f $ApiPort, $FrontendPort) -ForegroundColor Yellow
    exit 1
}

Set-Location $RepoRoot

Write-Host "ElectricalStore local launcher" -ForegroundColor Cyan
Write-Host ("Repo: {0}" -f $RepoRoot)
Write-Host ("Official ports: API {0} / Frontend {1}" -f $ApiPort, $FrontendPort)
Write-Host ""

Stop-LauncherOwnedPreviousRun
Assert-PortsFree

$apiCommand = "Set-Location -LiteralPath '$RepoRoot'; Write-Host 'ElectricalStore API - http://localhost:$ApiPort' -ForegroundColor Cyan; dotnet run --project src/ElectricalStore.Api --launch-profile http"
$frontendDir = Join-Path $RepoRoot "frontend"
# npm run dev uses package.json script with --port 3100 (no silent port fallback).
$frontendCommand = "Set-Location -LiteralPath '$frontendDir'; Write-Host 'ElectricalStore frontend - http://localhost:$FrontendPort' -ForegroundColor Cyan; npm.cmd run dev -- --port $FrontendPort"

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
    repoRoot         = $RepoRoot
}
$state | ConvertTo-Json | Set-Content -LiteralPath $StateFile -Encoding utf8

Write-Host "Started in separate windows:" -ForegroundColor Green
Write-Host ("  API shell PID:      {0}" -f $apiProc.Id)
Write-Host ("  Frontend shell PID: {0}" -f $frontendProc.Id)
Write-Host "  State file:         .dev-processes.json (gitignored)"
Write-Host ""
Write-Host ("Frontend: http://localhost:{0}" -f $FrontendPort)
Write-Host ("Admin:    http://localhost:{0}/admin" -f $FrontendPort)
Write-Host ("Backend:  http://localhost:{0}" -f $ApiPort)
Write-Host ("Health:   http://localhost:{0}/health" -f $ApiPort)
Write-Host ("Swagger:  http://localhost:{0}/swagger" -f $ApiPort)
Write-Host ""
Write-Host "Note: Do not also run Visual Studio debugging of ElectricalStore.Api on the same port." -ForegroundColor DarkYellow
Write-Host "Stop with: .\stop-dev.ps1"
Write-Host "Re-run .\dev.ps1 anytime to restart launcher-owned processes."
