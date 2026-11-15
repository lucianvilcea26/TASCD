#!/bin/sh
# Creates .env with random secrets (only if it does not exist yet), then renders the realm.
#   ./init-env.sh [domain]      default domain: lucianvilcea.ro
set -eu
cd "$(dirname "$0")"
if [ ! -f .env ]; then
  rnd() { openssl rand -base64 33 | tr -d '/+=' | cut -c1-32; }
  umask 077
  {
    echo "DOMAIN=${1:-lucianvilcea.ro}"
    echo "KC_ADMIN_PASSWORD=$(rnd)"
    echo "POSTGRES_PASSWORD=$(rnd)"
    echo "SECRETARIAT_CLIENT_SECRET=$(rnd)"
    echo "BIBLIOTECA_CLIENT_SECRET=$(rnd)"
  } > .env
  echo "Created .env (the Keycloak admin password is KC_ADMIN_PASSWORD)."
fi
# Staff passwords: the lab ones are public in the README, so the server gets its own.
for key in SECRETARA_PASSWORD BIBLIOTECAR_PASSWORD; do
  grep -q "^$key=" .env || echo "$key=$(openssl rand -base64 18 | tr -d '/+=')" >> .env
done
python3 render-realm.py
