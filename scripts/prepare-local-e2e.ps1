# Prepare local Playwright E2E credentials (Development only).
# Writes user-secrets + frontend/.env.e2e.local - never commit either.
# Account/role creation happens on next API start via LocalDevAccountFixtureSeeder.
#Requires -Version 5.1

param(
  [string]$ApiBaseUrl = "http://localhost:5180",
  [switch]$SkipHealthCheck
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$ApiProject = Join-Path $Root "src/ElectricalStore.Api/ElectricalStore.Api.csproj"
$EnvFilePath = Join-Path $Root "frontend/.env.e2e.local"

$Defaults = @{
  CustomerEmail    = "e2e.customer@electricalstore.local"
  CustomerUserName = "e2ecustomer"
  AdminEmail       = "e2e.admin@electricalstore.local"
  AdminUserName    = "e2eadmin"
  LimitedEmail     = "e2e.limited@electricalstore.local"
  LimitedUserName  = "e2elimited"
}

function New-StrongE2EPassword {
  $bytes = New-Object byte[] 24
  $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
  try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
  return ("E2e!" + [Convert]::ToBase64String($bytes)).Replace("+", "A").Replace("/", "B").Replace("=", "C")
}

function Read-EnvFileValue([string]$Path, [string]$Key) {
  if (-not (Test-Path -LiteralPath $Path)) { return $null }
  foreach ($line in Get-Content -LiteralPath $Path) {
    $trimmed = $line.Trim()
    if (-not $trimmed -or $trimmed.StartsWith("#")) { continue }
    $eq = $trimmed.IndexOf("=")
    if ($eq -le 0) { continue }
    $k = $trimmed.Substring(0, $eq).Trim()
    if ($k -ne $Key) { continue }
    $v = $trimmed.Substring($eq + 1).Trim()
    if (($v.StartsWith('"') -and $v.EndsWith('"')) -or ($v.StartsWith("'") -and $v.EndsWith("'"))) {
      $v = $v.Substring(1, $v.Length - 2)
    }
    if (-not [string]::IsNullOrWhiteSpace($v)) { return $v }
  }
  return $null
}

function Get-OrCreatePassword([string]$EnvName) {
  $existing = [Environment]::GetEnvironmentVariable($EnvName, "Process")
  if (-not [string]::IsNullOrWhiteSpace($existing)) { return $existing }
  $fromFile = Read-EnvFileValue $EnvFilePath $EnvName
  if (-not [string]::IsNullOrWhiteSpace($fromFile)) { return $fromFile }
  return New-StrongE2EPassword
}

function Set-UserSecret([string]$Key, [string]$Value) {
  & dotnet user-secrets set $Key $Value --project $ApiProject | Out-Null
  if ($LASTEXITCODE -ne 0) {
    throw "Failed to set user-secret '$Key'."
  }
}

Write-Host "ElectricalStore local E2E prepare" -ForegroundColor Cyan
Write-Host "API project: $ApiProject"

if (-not $SkipHealthCheck) {
  try {
    $health = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 5
    if ($health.StatusCode -ne 200) {
      throw "Unexpected health status $($health.StatusCode)"
    }
    Write-Host "[ok] API health $ApiBaseUrl/health" -ForegroundColor Green
  }
  catch {
    Write-Host "[warn] API not reachable at $ApiBaseUrl - secrets will still be written." -ForegroundColor DarkYellow
    Write-Host "       Start with .\dev.ps1, then re-run this script or restart API after secrets." -ForegroundColor DarkYellow
  }
}

$customerEmail = if ($env:E2E_CUSTOMER_EMAIL) { $env:E2E_CUSTOMER_EMAIL } else { $Defaults.CustomerEmail }
$customerUser = if ($env:E2E_CUSTOMER_USER) { $env:E2E_CUSTOMER_USER } else { $Defaults.CustomerUserName }
$adminEmail = if ($env:E2E_ADMIN_EMAIL) { $env:E2E_ADMIN_EMAIL } else { $Defaults.AdminEmail }
$adminUser = if ($env:E2E_ADMIN_USER) { $env:E2E_ADMIN_USER } else { $Defaults.AdminUserName }
$limitedEmail = if ($env:E2E_LIMITED_EMAIL) { $env:E2E_LIMITED_EMAIL } else { $Defaults.LimitedEmail }
$limitedUser = if ($env:E2E_LIMITED_USER) { $env:E2E_LIMITED_USER } else { $Defaults.LimitedUserName }

$customerPassword = Get-OrCreatePassword "E2E_CUSTOMER_PASSWORD"
$adminPassword = Get-OrCreatePassword "E2E_ADMIN_PASSWORD"
$limitedPassword = Get-OrCreatePassword "E2E_LIMITED_PASSWORD"

Write-Host "Writing LocalDevFixtures user-secrets (passwords not printed)..." -ForegroundColor Yellow
Set-UserSecret "LocalDevFixtures:Enabled" "true"
Set-UserSecret "LocalDevFixtures:CustomerEmail" $customerEmail
Set-UserSecret "LocalDevFixtures:CustomerUserName" $customerUser
Set-UserSecret "LocalDevFixtures:CustomerPassword" $customerPassword
Set-UserSecret "LocalDevFixtures:AdminEmail" $adminEmail
Set-UserSecret "LocalDevFixtures:AdminUserName" $adminUser
Set-UserSecret "LocalDevFixtures:AdminPassword" $adminPassword
Set-UserSecret "LocalDevFixtures:LimitedEmail" $limitedEmail
Set-UserSecret "LocalDevFixtures:LimitedUserName" $limitedUser
Set-UserSecret "LocalDevFixtures:LimitedPassword" $limitedPassword

$envLines = @(
  "E2E_BASE_URL=http://localhost:3100",
  "E2E_API_URL=http://localhost:5180",
  "E2E_CUSTOMER_EMAIL=$customerEmail",
  "E2E_CUSTOMER_PASSWORD=$customerPassword",
  "E2E_ADMIN_EMAIL=$adminEmail",
  "E2E_ADMIN_PASSWORD=$adminPassword",
  "E2E_LIMITED_EMAIL=$limitedEmail",
  "E2E_LIMITED_PASSWORD=$limitedPassword"
)
($envLines -join "`n") + "`n" | Set-Content -LiteralPath $EnvFilePath -Encoding utf8 -NoNewline

Write-Host ""
Write-Host "[ok] Wrote gitignored $EnvFilePath" -ForegroundColor Green
Write-Host "[ok] User-secrets updated for LocalDevFixtures accounts" -ForegroundColor Green
Write-Host ""
Write-Host "Emails (passwords only in .env.e2e.local / user-secrets):" -ForegroundColor Cyan
Write-Host "  Customer: $customerEmail"
Write-Host "  Admin:    $adminEmail"
Write-Host "  Limited:  $limitedEmail"
Write-Host ""
Write-Host "NEXT: restart local API so LocalDevAccountFixtureSeeder runs:" -ForegroundColor Yellow
Write-Host "  .\stop-dev.ps1"
Write-Host "  .\dev.ps1"
Write-Host "Then: cd frontend; npm run test:e2e"
Write-Host ""
Write-Host "Production: LocalDevFixtures:Enabled must remain false (appsettings.json default)." -ForegroundColor DarkYellow