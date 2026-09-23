# Seeds the Development DemoCatalog into the local API database.
# Idempotent: safe to re-run. Does not wipe LocalDevFixtures or arbitrary catalog rows.
#
# Prerequisites:
# - DemoCatalog:Enabled=true in appsettings.Development.json (or env DemoCatalog__Enabled=true)
# - Local MSSQL ElectricalStore.Db reachable
# - API not required to be stopped if you only want to trigger via restart
#
# Usage:
#   .\scripts\seed-demo-catalog.ps1
#
# This script restarts/starts the API briefly so DevelopmentInitialization runs the seeder.
# Prefer stopping the API first if port 5180 is busy.

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

Write-Host "DemoCatalog seed via Development API startup..." -ForegroundColor Cyan
Write-Host "Ensure DemoCatalog:Enabled=true (Development). Existing DEMO-* rows are skipped." -ForegroundColor DarkCyan

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5180"
$env:DemoCatalog__Enabled = "true"

$proj = Join-Path $repoRoot "src\ElectricalStore.Api\ElectricalStore.Api.csproj"

# Run until listening, then stop — seeding happens during InitializeDevelopmentAsync.
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = "dotnet"
$psi.Arguments = "run --project `"$proj`" --no-launch-profile"
$psi.WorkingDirectory = $repoRoot
$psi.UseShellExecute = $false
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true
$psi.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development"
$psi.EnvironmentVariables["ASPNETCORE_URLS"] = "http://localhost:5180"
$psi.EnvironmentVariables["DemoCatalog__Enabled"] = "true"

$p = [System.Diagnostics.Process]::Start($psi)
$deadline = [DateTime]::UtcNow.AddMinutes(3)
$ready = $false
try {
  while (-not $p.HasExited -and [DateTime]::UtcNow -lt $deadline) {
    try {
      $r = Invoke-WebRequest "http://localhost:5180/health" -UseBasicParsing -TimeoutSec 2
      if ($r.StatusCode -eq 200) { $ready = $true; break }
    } catch { }
    Start-Sleep -Seconds 2
  }
}
finally {
  if (-not $p.HasExited) {
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
  }
}

if ($ready) {
  Write-Host "DemoCatalog seed pass completed (API reached /health). Check API logs for DemoCatalog counts." -ForegroundColor Green
} else {
  Write-Host "API did not become healthy in time. Inspect build/run output; seeding may still have run during startup." -ForegroundColor Yellow
  exit 1
}
