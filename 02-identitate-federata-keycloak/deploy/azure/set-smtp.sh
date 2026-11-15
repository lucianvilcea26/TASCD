#!/bin/sh
# Makes Keycloak send real email (invitations, password resets) through an SMTP server
# instead of Mailpit. Asks for the mailbox password without echoing it, keeps it in smtp.env
# (readable only by this user), updates the running Keycloak and can send a test message.
#   ./set-smtp.sh            interactive
#   ./set-smtp.sh --off      back to Mailpit
set -eu
cd "$(dirname "$0")"
. ./.env

. ./kc.sh

apply() {  # push the realm's smtpServer block to the running Keycloak
  python3 render-realm.py --smtp-json | kcadm_in update realms/universitate -f -
}

if [ "${1:-}" = "--off" ]; then
  rm -f smtp.env
  ask_admin
  printf '{"smtpServer":{"host":"mailpit","port":"1025","from":"noreply@universitate.local","fromDisplayName":"Universitate","auth":"false","ssl":"false","starttls":"false"}}' |
    kcadm_in update realms/universitate -f -
  python3 render-realm.py
  echo "Keycloak sends email to Mailpit again."
  exit 0
fi

printf "SMTP host [mail.%s]: " "$DOMAIN"; read -r host; host=${host:-mail.$DOMAIN}
printf "Port, 587 (STARTTLS) or 465 (SSL) [587]: "; read -r port; port=${port:-587}
printf "Mailbox (user and From address) [noreply@%s]: " "$DOMAIN"; read -r user; user=${user:-noreply@$DOMAIN}
printf "Mailbox password (not shown): "; stty -echo; read -r pass; stty echo; echo

umask 077
{
  echo "SMTP_HOST=$host"
  echo "SMTP_PORT=$port"
  echo "SMTP_USER=$user"
  echo "SMTP_FROM=$user"
  echo "SMTP_PASSWORD=$pass"
} > smtp.env

python3 render-realm.py
ask_admin
apply
echo "Keycloak now sends email through $host:$port as $user."

printf "Send a test message to (empty to skip): "; read -r to
if [ -n "$to" ]; then
  python3 - "$to" <<'PY'
import smtplib, ssl, sys
from email.message import EmailMessage
from email.utils import formatdate, make_msgid
env = dict(l.split("=", 1) for l in open("smtp.env").read().splitlines() if l.strip())
msg = EmailMessage()
msg["From"], msg["To"], msg["Subject"] = f"Universitate (demo) <{env['SMTP_FROM']}>", sys.argv[1], "Test SMTP Keycloak"
# Without Date and Message-ID the receiving server adds them, and spam filters notice.
msg["Date"] = formatdate(localtime=True)
msg["Message-ID"] = make_msgid(domain=env["SMTP_FROM"].split("@")[1])
msg.set_content("Mesaj de test: serverul laboratorului poate trimite emailuri prin " + env["SMTP_HOST"] + ".")
ctx = ssl.create_default_context()
if env["SMTP_PORT"] == "465":
    s = smtplib.SMTP_SSL(env["SMTP_HOST"], 465, context=ctx, timeout=20)
else:
    s = smtplib.SMTP(env["SMTP_HOST"], int(env["SMTP_PORT"]), timeout=20); s.starttls(context=ctx)
s.login(env["SMTP_USER"], env["SMTP_PASSWORD"]); s.send_message(msg); s.quit()
print("Test message sent to", sys.argv[1])
PY
fi
