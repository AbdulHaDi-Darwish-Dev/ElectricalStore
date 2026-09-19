# Development-only helper for Windows local setup.
# Idempotent by default: preserves existing JWT keys and Owner password.
# Optional: -RotateJwt  -RotateOwnerPassword
param(
  [switch]$RotateJwt,
  [switch]$RotateOwnerPassword
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$ApiProject = Join-Path $Root "src/ElectricalStore.Api/ElectricalStore.Api.csproj"
$UserSecretsId = "ElectricalStore-dev-secrets"
$SecretsPath = Join-Path $env:APPDATA "Microsoft\UserSecrets\$UserSecretsId\secrets.json"

$WindowsDevConnectionString =
  "Server=DESKTOP-30CDIBP\MSSQLSERVER22;Database=ElectricalStore.Db;Trusted_Connection=True;TrustServerCertificate=True;"

$JwtIssuer = "https://electricalstore.local"
$JwtAudience = "electricalstore-api"
$OwnerEmail = "owner@electricalstore.local"
$OwnerUserName = "owner"
$LegacyWeakOwnerPassword = "ChangeMe-Owner-1!"

Write-Host "ElectricalStore development setup (Windows)"
Write-Host "Project: $ApiProject"
Write-Host ""

dotnet user-secrets init --project $ApiProject 2>$null | Out-Null

function Get-SecretsObject {
  if (-not (Test-Path $SecretsPath)) {
    return [pscustomobject]@{}
  }
  $raw = Get-Content -LiteralPath $SecretsPath -Raw -ErrorAction SilentlyContinue
  if ([string]::IsNullOrWhiteSpace($raw)) {
    return [pscustomobject]@{}
  }
  return $raw | ConvertFrom-Json
}

function Get-SecretValue([string]$Key) {
  $obj = Get-SecretsObject
  $prop = $obj.PSObject.Properties[$Key]
  if ($null -eq $prop) { return $null }
  $value = [string]$prop.Value
  if ([string]::IsNullOrWhiteSpace($value)) { return $null }
  return $value
}

function Set-SecretValue([string]$Key, [string]$Value) {
  dotnet user-secrets set $Key $Value --project $ApiProject | Out-Null
}

function Test-SecretPresent([string]$Key) {
  return $null -ne (Get-SecretValue $Key)
}

function Get-DevJwtPems {
  $rsa = [System.Security.Cryptography.RSA]::Create(2048)
  try {
    $exportPem = $rsa.GetType().GetMethod("ExportPkcs8PrivateKeyPem")
    if ($null -ne $exportPem) {
      return @{
        Private = $rsa.ExportPkcs8PrivateKeyPem()
        Public = $rsa.ExportSubjectPublicKeyInfoPem()
      }
    }
  }
  finally {
    $rsa.Dispose()
  }

  $opensslCandidates = @(
    "openssl",
    "C:\Program Files\Git\usr\bin\openssl.exe",
    "C:\Program Files (x86)\Git\usr\bin\openssl.exe"
  )
  $openssl = $opensslCandidates | Where-Object {
    if ($_ -eq "openssl") {
      $null -ne (Get-Command openssl -ErrorAction SilentlyContinue)
    } else {
      Test-Path $_
    }
  } | Select-Object -First 1

  if (-not $openssl) {
    throw "Unable to generate JWT PEMs. Install PowerShell 7+ (ExportPkcs8PrivateKeyPem) or OpenSSL (e.g. Git for Windows)."
  }

  $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ("electricalstore-dev-jwt-" + [Guid]::NewGuid().ToString("N"))
  New-Item -ItemType Directory -Path $tempDir | Out-Null
  try {
    $keyPath = Join-Path $tempDir "jwt.key"
    $pubPath = Join-Path $tempDir "jwt.pub"
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
      & $openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out $keyPath 2>$null | Out-Null
      if ($LASTEXITCODE -ne 0) { throw "openssl genpkey failed." }
      & $openssl rsa -in $keyPath -pubout -out $pubPath 2>$null | Out-Null
      if ($LASTEXITCODE -ne 0) { throw "openssl rsa -pubout failed." }
    }
    finally {
      $ErrorActionPreference = $prevEap
    }

    return @{
      Private = [System.IO.File]::ReadAllText($keyPath).Trim() + "`n"
      Public = [System.IO.File]::ReadAllText($pubPath).Trim() + "`n"
    }
  }
  finally {
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
  }
}

function New-DevOwnerPassword {
  $bytes = New-Object byte[] 32
  $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
  try {
    $rng.GetBytes($bytes)
  }
  finally {
    $rng.Dispose()
  }

  # Strong random local credential (not for production). Prefix/suffix help Identity complexity rules.
  $core = [Convert]::ToBase64String($bytes)
  return "Es1!$core"
}

# --- Database (Windows Trusted Connection; no SQL password) ---
Set-SecretValue "ConnectionStrings:Default" $WindowsDevConnectionString
Write-Host "[ok] Database configuration configured (Windows Trusted Connection → ElectricalStore.Db)"

# --- Non-secret identities (safe to sync) ---
Set-SecretValue "Permixa:Jwt:Issuer" $JwtIssuer
Set-SecretValue "Permixa:Jwt:Audience" $JwtAudience
Set-SecretValue "Permixa:Bootstrap:OwnerEmail" $OwnerEmail
Set-SecretValue "Permixa:Bootstrap:OwnerUserName" $OwnerUserName
Set-SecretValue "Permixa:Bootstrap:Enabled" "true"
Write-Host "[ok] JWT issuer/audience and bootstrap Owner identity configured"

# --- JWT keys (preserve unless missing or -RotateJwt) ---
$hasPrivate = Test-SecretPresent "Permixa:Jwt:PrivateKeyPem"
$hasPublic = Test-SecretPresent "Permixa:Jwt:PublicKeyPem"
if ($RotateJwt -or -not $hasPrivate -or -not $hasPublic) {
  $pems = Get-DevJwtPems
  Set-SecretValue "Permixa:Jwt:PrivateKeyPem" $pems.Private
  Set-SecretValue "Permixa:Jwt:PublicKeyPem" $pems.Public
  if ($RotateJwt) {
    Write-Host "[ok] JWT keys regenerated (-RotateJwt)"
  } else {
    Write-Host "[ok] JWT keys generated"
  }
} else {
  Write-Host "[ok] JWT keys already exist (preserved)"
}

# --- Owner password (generate once; preserve thereafter) ---
$existingPassword = Get-SecretValue "Permixa:Bootstrap:OwnerPassword"
$shouldGeneratePassword =
  $RotateOwnerPassword -or
  [string]::IsNullOrWhiteSpace($existingPassword) -or
  ($existingPassword -eq $LegacyWeakOwnerPassword)

if ($shouldGeneratePassword) {
  $newPassword = New-DevOwnerPassword
  Set-SecretValue "Permixa:Bootstrap:OwnerPassword" $newPassword
  Write-Host ""
  Write-Host "=== LOCAL DEVELOPMENT CREDENTIAL (shown once) ==="
  Write-Host "Owner username : $OwnerUserName"
  Write-Host "Owner email    : $OwnerEmail"
  Write-Host "Owner password : $newPassword"
  Write-Host "Store this password securely. It is not committed to Git."
  Write-Host "================================================="
  Write-Host ""
  if ($existingPassword -eq $LegacyWeakOwnerPassword) {
    Write-Host "[ok] Owner credential upgraded from legacy weak default"
  } elseif ($RotateOwnerPassword) {
    Write-Host "[ok] Owner credential regenerated (-RotateOwnerPassword)"
    Write-Host "[warn] If Owner already exists in the database, rotating this secret does NOT change the DB password."
  } else {
    Write-Host "[ok] Owner credential generated"
  }
} else {
  Write-Host "[ok] Owner credential already exists (preserved)"
}

Write-Host ""
Write-Host "Setup complete."
Write-Host "Next: dotnet run --project src/ElectricalStore.Api"
Write-Host "After first successful Owner login:"
Write-Host "  dotnet user-secrets set `"Permixa:Bootstrap:Enabled`" `"false`" --project src/ElectricalStore.Api"
Write-Host "  dotnet user-secrets remove `"Permixa:Bootstrap:OwnerPassword`" --project src/ElectricalStore.Api"
