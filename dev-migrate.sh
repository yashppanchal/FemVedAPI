#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# dev-migrate.sh — Apply EF Core migrations using .env.local
#
# Usage:
#   ./dev-migrate.sh                  # apply all pending migrations
#   ./dev-migrate.sh InitialCreate    # migrate to a specific migration name
# ─────────────────────────────────────────────────────────────────────────────

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env.local"

if [ ! -f "$ENV_FILE" ]; then
  echo "ERROR: .env.local not found at $SCRIPT_DIR"
  echo "Copy .env.example to .env.local and fill in your values."
  exit 1
fi

echo "Loading environment from .env.local..."

# Parse .env.local robustly:
#  - skip blank lines and comment lines
#  - skip lines that have no '='
#  - strip surrounding double-quotes from values
#  - validate key is a legal identifier (letters, digits, underscores only)
while IFS= read -r line || [[ -n "$line" ]]; do
  line="${line%$'\r'}"  # strip Windows CRLF carriage return
  # skip blank / whitespace-only lines
  [[ -z "${line//[[:space:]]/}" ]] && continue
  # skip comment lines (may start with optional whitespace then #)
  [[ "$line" =~ ^[[:space:]]*# ]]  && continue
  # skip lines that have no = at all
  [[ "$line" != *"="* ]]           && continue

  key="${line%%=*}"           # everything before first '='
  val="${line#*=}"            # everything after  first '='

  # strip surrounding double-quotes from value
  if [[ "$val" == '"'*'"' ]]; then
    val="${val#\"}"   # strip leading "
    val="${val%\"}"   # strip trailing "
  fi

  # only export if key looks like a valid shell identifier
  if [[ "$key" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
    export "$key=$val"
  fi
done < "$ENV_FILE"

echo "DB host: $(echo "$DB_CONNECTION_STRING" | grep -oP 'Host=[^;]+')"

TARGET="${1:-}"

if [ -z "$TARGET" ]; then
  echo "Applying all pending migrations..."
  dotnet ef database update \
    --project "$SCRIPT_DIR/src/FemVed.Infrastructure" \
    --startup-project "$SCRIPT_DIR/src/FemVed.API"
else
  echo "Migrating to: $TARGET"
  dotnet ef database update "$TARGET" \
    --project "$SCRIPT_DIR/src/FemVed.Infrastructure" \
    --startup-project "$SCRIPT_DIR/src/FemVed.API"
fi

echo "Migrations complete."
