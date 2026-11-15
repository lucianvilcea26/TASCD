#!/bin/sh
# Resets the lab to a fresh state before a class.
#   ./reset-lab.sh        deletes the realm "universitate" (users, sessions, events) and re-imports it,
#                         and empties both app databases. The master realm, with your permanent
#                         administrator, stays. Asks for that administrator's login.
#   ./reset-lab.sh --all  deletes the whole Keycloak database too; Keycloak starts again with the
#                         temporary "admin" (KC_ADMIN_PASSWORD), so run ./make-admin.sh afterwards.
# Both keep .env, smtp.env and Caddy's certificates (docker compose down -v would delete them,
# and Let's Encrypt limits re-issuing).
set -eu
cd "$(dirname "$0")"
. ./.env
. ./kc.sh
python3 render-realm.py

if [ "${1:-}" = "--all" ]; then
  docker compose rm -sf keycloak postgres secretariat biblioteca mailpit
  volumes="keycloak-db secretariat-data secretariat-keys biblioteca-data biblioteca-keys"
else
  ask_admin
  kcadm delete realms/universitate
  echo "Deleted the realm universitate."
  docker compose rm -sf secretariat biblioteca mailpit
  volumes="secretariat-data secretariat-keys biblioteca-data biblioteca-keys"
  docker compose restart keycloak   # --import-realm brings universitate back from realm/
fi
for v in $volumes; do
  docker volume rm -f "universitate_$v" >/dev/null
done
docker compose up -d
echo "Lab reset. Keycloak needs about a minute to import the realm."
