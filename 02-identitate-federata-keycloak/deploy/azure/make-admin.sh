#!/bin/sh
# Replaces Keycloak's temporary bootstrap admin ("admin", password KC_ADMIN_PASSWORD in .env)
# with a permanent administrator whose password you choose. Run it once, after the first start:
#   ./make-admin.sh
# KC_BOOTSTRAP_ADMIN_* in docker-compose.yml only act when the master realm is created, so the
# temporary account does not come back on restarts. reset-lab.sh keeps the master realm too.
set -eu
cd "$(dirname "$0")"
. ./.env
. ./kc.sh
NEW_PASS=""; export NEW_PASS

# 1. Log in as the temporary admin.
KC_USER=admin KC_PASS=$KC_ADMIN_PASSWORD; export KC_USER KC_PASS
if ! kcadm get users -r master -q username=admin -q exact=true --fields id >/dev/null 2>&1; then
  echo "The temporary account 'admin' no longer works (already replaced?). Nothing to do."
  exit 1
fi

# 2. The new administrator.
printf "Username of the permanent administrator: "; read -r user
case "$user" in ""|admin) echo "Choose a name other than 'admin'."; exit 1 ;; esac
printf "Email (optional, for password resets): "; read -r email
printf "Password (not shown, at least 12 characters): "; stty -echo; read -r p1; stty echo; echo
printf "Again: "; stty -echo; read -r p2; stty echo; echo
[ "$p1" = "$p2" ] || { echo "The passwords differ."; exit 1; }
[ ${#p1} -ge 12 ] || { echo "Too short."; exit 1; }

if [ -n "$email" ]; then
  kcadm create users -r master -s "username=$user" -s enabled=true -s "email=$email" -s emailVerified=true >/dev/null
else
  kcadm create users -r master -s "username=$user" -s enabled=true >/dev/null
fi
NEW_PASS=$p1 kcadm --set-new-password master "$user"
kcadm add-roles -r master --uusername "$user" --rolename admin
echo "Created '$user' with the realm role admin."

# 3. Prove the new account works before removing the old one.
if ! KC_USER=$user KC_PASS=$p1 kcadm get realms/master --fields realm >/dev/null 2>&1; then
  echo "The new account cannot log in; the temporary admin was kept."; exit 1
fi

# 4. Delete the temporary admin (logged in as the new one).
KC_USER=$user KC_PASS=$p1
old=$(kcadm get users -r master -q username=admin -q exact=true --fields id --format csv --noquotes)
kcadm delete "users/$old" -r master
echo "Deleted the temporary 'admin'. Log in to https://idp.$DOMAIN/admin as '$user'."
echo "Recommended: in the account console of '$user', add an authenticator app (Signing in → Authenticator application)."
