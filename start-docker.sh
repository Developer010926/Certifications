#!/bin/sh

set -u

fail() {
  printf 'ERROR: %s\n' "$1" >&2
  exit 1
}

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd) || fail "Cannot resolve the project directory."
cd "$script_dir" || fail "Cannot switch to the project directory."

command -v docker >/dev/null 2>&1 || fail "Docker CLI was not found in PATH."
docker info >/dev/null 2>&1 || fail "Docker is not running."
command -v node >/dev/null 2>&1 || fail "Node.js was not found in PATH."
command -v dotnet >/dev/null 2>&1 || fail ".NET SDK was not found in PATH."

[ -f .env ] || fail ".env was not found. Create it with: cp .env.example .env"

if ! node frontend/certifications-ui/scripts/prepare-dev-https.mjs; then
  fail "The trusted localhost HTTPS certificate could not be prepared."
fi

if ! docker compose config --quiet; then
  fail "Docker Compose configuration is invalid."
fi

read_env_value() {
  awk -v key="$1" '
    index($0, key "=") == 1 {
      value = substr($0, length(key) + 2)
      sub(/\r$/, "", value)
    }
    END { print value }
  ' .env
}

postgres_db=$(read_env_value POSTGRES_DB)
postgres_user=$(read_env_value POSTGRES_USER)
postgres_port=$(read_env_value POSTGRES_PORT)
backend_port=$(read_env_value BACKEND_PORT)
frontend_port=$(read_env_value FRONTEND_PORT)

[ -n "$postgres_port" ] || postgres_port=5432
[ -n "$backend_port" ] || backend_port=5081
[ -n "$frontend_port" ] || frontend_port=4200

printf 'Connection string: Host=localhost;Port=%s;Database=%s;Username=%s;Password=<redacted>\n' \
  "$postgres_port" "$postgres_db" "$postgres_user"
printf 'Backend: http://localhost:%s\n' "$backend_port"
printf 'Frontend: https://localhost:%s\n' "$frontend_port"

if ! docker compose up --build --detach; then
  fail "Docker Compose failed to start."
fi

docker compose ps || fail "Docker Compose could not report service status."
