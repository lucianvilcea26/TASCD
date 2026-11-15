# Laboratorul pe un server public

Aceeași configurație ca în `deploy/`, pe o singură mașină Linux, în spatele HTTPS:

| Adresă | Serviciu |
|---|---|
| `https://idp.<domeniu>` | Keycloak (consola: `/admin`) |
| `https://secretariat.<domeniu>` | Secretariat |
| `https://biblioteca.<domeniu>` | Biblioteca |
| `https://platformacursuri.<domeniu>` | platforma de cursuri din exercițiile 1-16 (conturi locale, înregistrare și roluri, fără Keycloak) |

Caddy obține singur certificatele Let's Encrypt și trimite cererile către containere. Keycloak rulează în mod de producție (`start`), cu numele public fixat prin `KC_HOSTNAME`. Mailpit nu este public; îl deschidem printr-un tunel SSH.

## Ce diferă față de laborator

* **Adresele.** `render-realm.py` pornește de la `deploy/keycloak/realm-universitate.json` și înlocuiește `http://localhost:5001` și `http://localhost:5002` cu adresele publice. Back-channel logout-ul merge direct la containere (`http://secretariat:8080/backchannel-logout`).
* **Secretele.** Parola contului `admin`, parola bazei de date, secretele celor doi clienți și parolele conturilor `secretara` și `bibliotecar` sunt generate aleator de `init-env.sh` în `.env`. Parolele din README-ul laboratorului nu funcționează pe server, fiindcă sunt publice.
* **Aplicațiile** sunt construite din `src/` cu `Dockerfile` și primesc configurația prin variabile de mediu (`Keycloak__Authority`, `Links__Biblioteca` etc.). În interiorul rețelei Docker, numele publice indică tot spre Caddy, așa că aplicațiile ajung la Keycloak prin aceleași adrese HTTPS ca browserul.

## Pornirea

Pe server sunt necesare Docker și înregistrări DNS de tip A pentru `idp`, `secretariat`, `biblioteca` și `platformacursuri`, toate spre adresa serverului.

**Platforma de cursuri** se construiește din folderul `01-autentificare-locala/platforma-cursuri`. Dacă pe server este copiat doar acest folder al laboratorului, copiem alături și platforma și indicăm calea ei în `.env`, de exemplu `CURSURI_SRC=/home/azureuser/platforma-cursuri`. Platforma nu are bază de date: cursurile, conturile și registrul simulat sunt date de test, în memorie, cu parola din exercițiul 12.

```bash
cd 02-identitate-federata-keycloak/deploy/azure
./init-env.sh lucianvilcea.ro
docker compose up -d --build
```

**Administratorul permanent.** La prima pornire, Keycloak creează un cont temporar, `admin`, cu parola `KC_ADMIN_PASSWORD` din `.env`, iar consola afișează avertismentul „temporary admin”. O singură dată, după prima pornire, rulăm:

```bash
ssh -t azureuser@<server> "cd ~/universitate/deploy/azure && ./make-admin.sh"
```

Scriptul creează un administrator cu numele și parola alese de noi, verifică faptul că se poate autentifica și abia apoi șterge contul temporar. Contul temporar nu revine la reporniri: variabilele `KC_BOOTSTRAP_ADMIN_*` acționează doar când realm-ul `master` este creat. De aici înainte, `set-smtp.sh` și `reset-lab.sh` cer datele administratorului permanent.

Parolele generate le citim pe server:

```bash
grep PASSWORD .env
```

Mailpit, prin tunel SSH, de pe calculatorul nostru:

```bash
ssh -L 8025:127.0.0.1:8025 azureuser@<server>
```

apoi `http://localhost:8025` în browser.

**Emailuri reale.** Implicit, Keycloak trimite invitațiile la Mailpit. Ca să plece la destinatari, creăm o căsuță de email pe domeniu (de exemplu `noreply@lucianvilcea.ro`, în cPanel) și rulăm `./set-smtp.sh`. Scriptul cere parola căsuței fără s-o afișeze, o păstrează în `smtp.env`, actualizează Keycloak-ul pornit și poate trimite un mesaj de test. Azure blochează portul 25, deci folosim 587 (STARTTLS) sau 465 (SSL). `./set-smtp.sh --off` revine la Mailpit.

**Resetarea înainte de oră**: `./reset-lab.sh` șterge realm-ul `universitate` (utilizatori, sesiuni, evenimente) și îl reimportă, apoi golește bazele de date ale aplicațiilor. Realm-ul `master`, cu administratorul permanent, rămâne. `./reset-lab.sh --all` șterge și baza de date a lui Keycloak; după el rulăm din nou `./make-admin.sh`. Ambele păstrează `.env`, `smtp.env` și certificatele obținute de Caddy. Nu folosim `docker compose down -v`, pentru că ar șterge certificatele, iar Let's Encrypt limitează reemiterea lor.
