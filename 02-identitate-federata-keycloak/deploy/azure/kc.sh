# Sourced by the other scripts: runs kcadm.sh inside the Keycloak container.
# Credentials travel as environment variables (KC_USER, KC_PASS, NEW_PASS), never as
# command-line arguments on the host, so they do not show up in `ps`.

NEW_PASS=${NEW_PASS:-}; export NEW_PASS

ask_admin() {  # asks for the permanent Keycloak administrator (realm master)
  printf "Keycloak administrator (realm master): "; read -r KC_USER
  printf "Password (not shown): "; stty -echo; read -r KC_PASS; stty echo; echo
  export KC_USER KC_PASS
}

_kcadm() {
  docker compose exec -T -e KC_USER -e KC_PASS -e NEW_PASS keycloak sh -c '
    K=/opt/keycloak/bin/kcadm.sh
    $K config credentials --server http://localhost:8080 --realm master \
       --user "$KC_USER" --password "$KC_PASS" >/dev/null 2>&1 || { echo "Keycloak refused the administrator login." >&2; exit 3; }
    [ "$1" = "--set-new-password" ] && exec $K set-password -r "$2" --username "$3" --new-password "$NEW_PASS"
    exec $K "$@"' kcadm "$@"
}

# kcadm <args...>: logs in as $KC_USER, then runs kcadm.sh. Its stdin is /dev/null, so it never
# swallows what the user types at the script's next prompt.
kcadm() { _kcadm "$@" </dev/null; }

# kcadm_in <args...>: the same, but passes stdin through (for "-f -" with JSON on stdin).
kcadm_in() { _kcadm "$@"; }
