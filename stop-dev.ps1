# Stop processes started by .\dev.ps1 for this repository only.
# Official ports: API 5180 / Frontend 3100
# Usage (from repo root): .\stop-dev.ps1
# Does NOT kill unrelated processes occupying those ports.

$ErrorActionPreference = "Continue"
$RepoRoot = $PSScriptRoot
$StateFile = Join-Path $RepoRoot ".dev-processes.json"

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
    # /T = process tree (shell + dotnet/node children). Only this tracked PID.
    & taskkill.exe /PID $ProcessId /T /F 2>$null | Out-Null
    Start-Sleep -Milliseconds 400

    $still = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($still) {
        Write-Host ("  WARNING: {0} PID {1} still running." -f $Label, $ProcessId) -ForegroundColor Red
    }
    else {
        Write-Host ("  {0} stopped." -f $Label) -ForegroundColor Green
    }
}

Set-Location $RepoRoot

Write-Host "ElectricalStore local stop" -ForegroundColor Cyan

if (-not (Test-Path -LiteralPath $StateFile)) {
    Write-Host "No .dev-processes.json found - nothing tracked by the launcher to stop." -ForegroundColor DarkYellow
    Write-Host "If ports 5180/3100 are still busy, inspect PIDs manually (do not kill unrelated machine processes)." -ForegroundColor DarkYellow
    Write-Host "If Visual Studio owns the API, stop that debug session separately." -ForegroundColor DarkYellow
    exit 0
}

try {
    $state = Get-Content -LiteralPath $StateFile -Raw -Encoding utf8 | ConvertFrom-Json
}
catch {
    Write-Host ("ERROR: Could not read .dev-processes.json: {0}" -f $_) -ForegroundColor Red
    Write-Host "Removing unreadable state file." -ForegroundColor DarkYellow
    Remove-Item -LiteralPath $StateFile -Force -ErrorAction SilentlyContinue
    exit 1
}

$apiShellPid = 0
$frontendShellPid = 0
if ($null -ne $state.apiShellPid) {
    [void][int]::TryParse([string]$state.apiShellPid, [ref]$apiShellPid)
}
if ($null -ne $state.frontendShellPid) {
    [void][int]::TryParse([string]$state.frontendShellPid, [ref]$frontendShellPid)
}

Write-Host "Stopping launcher-tracked shells only:"
Stop-TrackedTree -ProcessId $apiShellPid -Label "API shell"
Stop-TrackedTree -ProcessId $frontendShellPid -Label "Frontend shell"

Remove-Item -LiteralPath $StateFile -Force -ErrorAction SilentlyContinue
Write-Host "Removed .dev-processes.json" -ForegroundColor Green
Write-Host "Done."
