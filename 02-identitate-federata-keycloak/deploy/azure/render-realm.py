"""Writes realm/realm-universitate.json: the lab realm with public HTTPS URLs and the secrets from .env.

The lab realm (deploy/keycloak/realm-universitate.json) stays the single source; only the client
URLs and secrets change. Keycloak calls the apps' back-channel logout endpoints inside the network.
"""
import json
import pathlib
import sys

here = pathlib.Path(__file__).resolve().parent


def smtp_server(env):
    """The realm's smtpServer block. Port 465 means implicit TLS, any other port STARTTLS."""
    implicit_tls = env.get("SMTP_PORT", "587") == "465"
    return {
        "host": env["SMTP_HOST"],
        "port": env.get("SMTP_PORT", "587"),
        "from": env["SMTP_FROM"],
        "fromDisplayName": "Universitate (demo)",
        "auth": "true",
        "user": env["SMTP_USER"],
        "password": env["SMTP_PASSWORD"],
        "ssl": "true" if implicit_tls else "false",
        "starttls": "false" if implicit_tls else "true",
    }


def read_env(path):
    if not path.exists():
        return {}
    return dict(line.split("=", 1) for line in path.read_text().splitlines() if line.strip() and not line.startswith("#"))


# .env is also read by docker compose; the SMTP password lives apart, in smtp.env, so that
# compose never tries to interpolate characters such as "$" in it.
env = read_env(here / ".env") | read_env(here / "smtp.env")

if sys.argv[1:] == ["--smtp-json"]:          # used by set-smtp.sh to update the running Keycloak
    print(json.dumps({"smtpServer": smtp_server(env)}))
    sys.exit()
domain = env["DOMAIN"]
apps = {
    "secretariat": ("5001", env["SECRETARIAT_CLIENT_SECRET"]),
    "biblioteca": ("5002", env["BIBLIOTECA_CLIENT_SECRET"]),
}

realm = json.loads((here.parent / "keycloak" / "realm-universitate.json").read_text(encoding="utf-8"))
for client in realm["clients"]:
    name = client["clientId"]
    if name not in apps:
        continue
    port, secret = apps[name]
    text = json.dumps(client)
    text = text.replace(f"http://host.docker.internal:{port}", f"http://{name}:8080")
    text = text.replace(f"http://localhost:{port}", f"https://{name}.{domain}")
    client.clear()
    client.update(json.loads(text))
    client["secret"] = secret

# The staff passwords in the lab realm are published in the README; a public server gets its own.
staff = {"secretara": env["SECRETARA_PASSWORD"], "bibliotecar": env["BIBLIOTECAR_PASSWORD"]}
for user in realm["users"]:
    if user["username"] in staff:
        for credential in user.get("credentials", []):
            if credential["type"] == "password":
                credential["value"] = staff[user["username"]]

# Real email (smtp.env, written by set-smtp.sh). Without SMTP_HOST the realm keeps sending to Mailpit.
if env.get("SMTP_HOST"):
    realm["smtpServer"] = smtp_server(env)

out = here / "realm" / "realm-universitate.json"
out.parent.mkdir(exist_ok=True)
out.write_text(json.dumps(realm, indent=2, ensure_ascii=False), encoding="utf-8")
print(f"Wrote {out.relative_to(here)} for {domain}")
