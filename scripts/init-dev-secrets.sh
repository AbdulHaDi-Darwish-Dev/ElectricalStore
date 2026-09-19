#!/usr/bin/env bash
set -euo pipefail

# Development-only helper for Linux/macOS (Docker SQL by default).
# Idempotent by default: preserves existing JWT keys and Owner password.
# Optional: --rotate-jwt  --rotate-owner-password
#
# Does NOT hardcode Windows named SQL instances.
# Override DB with: CONNECTION_STRING='Server=...;Database=...;...' ./scripts/init-dev-secrets.sh

ROTATE_JWT=0
ROTATE_OWNER_PASSWORD=0
for arg in "$@"; do
  case "$arg" in
    --rotate-jwt) ROTATE_JWT=1 ;;
    --rotate-owner-password) ROTATE_OWNER_PASSWORD=1 ;;
    -h|--help)
      echo "Usage: $0 [--rotate-jwt] [--rotate-owner-password]"
      exit 0
      ;;
  esac
done

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
API_PROJECT="$ROOT/src/ElectricalStore.Api/ElectricalStore.Api.csproj"
USER_SECRETS_ID="ElectricalStore-dev-secrets"
SECRETS_PATH="${HOME}/.microsoft/usersecrets/${USER_SECRETS_ID}/secrets.json"

JWT_ISSUER="https://electricalstore.local"
JWT_AUDIENCE="electricalstore-api"
OWNER_EMAIL="owner@electricalstore.local"
OWNER_USERNAME="owner"
LEGACY_WEAK_PASSWORD='ChangeMe-Owner-1!'

# Default: Docker Compose SQL from .env.example (sa password is a shared local-dev Docker default).
DEFAULT_CONNECTION_STRING="Server=localhost,1433;Database=ElectricalStore;User Id=sa;Password=Your_strong_DevOnly_Password123;TrustServerCertificate=True;Encrypt=False"
CONNECTION_STRING="${CONNECTION_STRING:-$DEFAULT_CONNECTION_STRING}"

echo "ElectricalStore development setup (Linux/macOS)"
echo "Project: $API_PROJECT"
echo ""

dotnet user-secrets init --project "$API_PROJECT" >/dev/null 2>&1 || true

if ! command -v openssl >/dev/null 2>&1; then
  echo "openssl not found. Install openssl or set Permixa:Jwt:* PEMs manually."
  exit 1
fi

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 not found (needed to read existing user-secrets safely)."
  exit 1
fi

get_secret() {
  local key="$1"
  SECRET_KEY="$key" SECRETS_PATH="$SECRETS_PATH" python3 - <<'PY'
import json, os
path = os.environ["SECRETS_PATH"]
key = os.environ["SECRET_KEY"]
if not os.path.isfile(path):
    raise SystemExit(0)
with open(path, encoding="utf-8") as f:
    data = json.load(f)
value = data.get(key)
if value:
    print(value, end="")
PY
}

set_secret() {
  local key="$1"
  local value="$2"
  dotnet user-secrets set "$key" "$value" --project "$API_PROJECT" >/dev/null
}

secret_present() {
  local value
  value="$(get_secret "$1" || true)"
  [[ -n "${value}" ]]
}

generate_owner_password() {
  # Strong random local credential (not for production).
  openssl rand -base64 32 | tr -d '\n' | awk '{print "Es1!" $0}'
}

# --- Database ---
set_secret "ConnectionStrings:Default" "$CONNECTION_STRING"
echo "[ok] Database configuration configured (override with CONNECTION_STRING=... if needed)"

# --- Non-secret identities ---
set_secret "Permixa:Jwt:Issuer" "$JWT_ISSUER"
set_secret "Permixa:Jwt:Audience" "$JWT_AUDIENCE"
set_secret "Permixa:Bootstrap:OwnerEmail" "$OWNER_EMAIL"
set_secret "Permixa:Bootstrap:OwnerUserName" "$OWNER_USERNAME"
set_secret "Permixa:Bootstrap:Enabled" "true"
echo "[ok] JWT issuer/audience and bootstrap Owner identity configured"

# --- JWT keys ---
if [[ "$ROTATE_JWT" -eq 1 ]] || ! secret_present "Permixa:Jwt:PrivateKeyPem" || ! secret_present "Permixa:Jwt:PublicKeyPem"; then
  TMPDIR="$(mktemp -d)"
  trap 'rm -rf "$TMPDIR"' EXIT
  openssl genrsa -out "$TMPDIR/private.pem" 2048 >/dev/null 2>&1
  openssl rsa -in "$TMPDIR/private.pem" -pubout -out "$TMPDIR/public.pem" >/dev/null 2>&1
  PRIVATE_KEY="$(cat "$TMPDIR/private.pem")"
  PUBLIC_KEY="$(cat "$TMPDIR/public.pem")"
  set_secret "Permixa:Jwt:PrivateKeyPem" "$PRIVATE_KEY"
  set_secret "Permixa:Jwt:PublicKeyPem" "$PUBLIC_KEY"
  if [[ "$ROTATE_JWT" -eq 1 ]]; then
    echo "[ok] JWT keys regenerated (--rotate-jwt)"
  else
    echo "[ok] JWT keys generated"
  fi
  rm -rf "$TMPDIR"
  trap - EXIT
else
  echo "[ok] JWT keys already exist (preserved)"
fi

# --- Owner password ---
EXISTING_PASSWORD="$(get_secret "Permixa:Bootstrap:OwnerPassword" || true)"
SHOULD_GENERATE=0
if [[ "$ROTATE_OWNER_PASSWORD" -eq 1 ]]; then
  SHOULD_GENERATE=1
elif [[ -z "$EXISTING_PASSWORD" ]]; then
  SHOULD_GENERATE=1
elif [[ "$EXISTING_PASSWORD" == "$LEGACY_WEAK_PASSWORD" ]]; then
  SHOULD_GENERATE=1
fi

if [[ "$SHOULD_GENERATE" -eq 1 ]]; then
  NEW_PASSWORD="$(generate_owner_password)"
  set_secret "Permixa:Bootstrap:OwnerPassword" "$NEW_PASSWORD"
  echo ""
  echo "=== LOCAL DEVELOPMENT CREDENTIAL (shown once) ==="
  echo "Owner username : $OWNER_USERNAME"
  echo "Owner email    : $OWNER_EMAIL"
  echo "Owner password : $NEW_PASSWORD"
  echo "Store this password securely. It is not committed to Git."
  echo "================================================="
  echo ""
  if [[ "$EXISTING_PASSWORD" == "$LEGACY_WEAK_PASSWORD" ]]; then
    echo "[ok] Owner credential upgraded from legacy weak default"
  elif [[ "$ROTATE_OWNER_PASSWORD" -eq 1 ]]; then
    echo "[ok] Owner credential regenerated (--rotate-owner-password)"
    echo "[warn] If Owner already exists in the database, rotating this secret does NOT change the DB password."
  else
    echo "[ok] Owner credential generated"
  fi
else
  echo "[ok] Owner credential already exists (preserved)"
fi

echo ""
echo "Setup complete."
echo "Next: dotnet run --project src/ElectricalStore.Api"
echo "After first successful Owner login:"
echo "  dotnet user-secrets set \"Permixa:Bootstrap:Enabled\" \"false\" --project src/ElectricalStore.Api"
echo "  dotnet user-secrets remove \"Permixa:Bootstrap:OwnerPassword\" --project src/ElectricalStore.Api"
