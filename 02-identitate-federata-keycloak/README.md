# Laboratorul 2: identitate federată cu Keycloak

Două aplicații .NET 10 Razor Pages ale unei universități fictive, care împart același furnizor de identitate, Keycloak. Laboratorul cursului **Identitate federată cu Keycloak** de pe [docs.lucianvilcea.ro](https://docs.lucianvilcea.ro); căile din paginile cursului sunt relative la acest folder.

| Aplicația | Adresa | Rolul ei |
|---|---|---|
| **Secretariat** | http://localhost:5001 | registrul studenților (sursa autoritară); creează conturile studenților **în Keycloak, prin API-ul REST de administrare**, trimite invitațiile și oferă un API pentru starea înmatriculării |
| **Biblioteca** | http://localhost:5002 | împrumută cărți doar studenților înmatriculați: din catalog, pe baza afirmației din jeton (versiunea A), sau la ghișeu, întrebând Secretariatul (versiunea B) |
| Keycloak | http://localhost:8080/admin (`admin` / `admin`) | furnizorul de identitate, cu realm-ul `universitate` |
| Mailpit | http://localhost:8025 | primește toate emailurile trimise de Keycloak, fără să le livreze |

Trei idei pe care laboratorul le face vizibile:

1. **Consola de administrare nu este furnizorul de identitate.** Este doar un client al API-ului REST de administrare. Secretariatul face aceleași operații din propria interfață.
2. **Dovedirea identității nu este autentificare.** Studenții nu se înregistrează singuri: datele lor vin din registru și nu le pot modifica. Linkul de invitație dovedește controlul adresei de email din registru, iar parola și aplicația de autentificare protejează autentificările următoare.
3. **Jetoanele sunt instantanee.** O afirmație ca `enrollment_status` este la fel de proaspătă ca jetonul. Deciziile care trebuie să fie actuale întreabă sursa autoritară, iar încheierea unei sesiuni trebuie anunțată aplicațiilor (deconectarea pe canalul din spate).

## Cerințe

* SDK-ul .NET 10
* Docker Desktop (pentru Keycloak 26.8.2, PostgreSQL 17 și Mailpit)
* o aplicație de autentificare: Google Authenticator, Microsoft Authenticator sau FreeOTP pe telefon, ori una pentru calculator, de exemplu KeePassXC sau Bitwarden.
  Funcționează complet fără rețea: telefonul doar scanează codul QR de pe ecran și nu comunică niciodată cu Keycloak.

## Pornire

```bash
git clone https://github.com/lucianvilcea26/TASCD.git
cd TASCD/02-identitate-federata-keycloak/deploy
docker compose up -d          # la prima pornire importă realm-ul; durează în jur de 30 de secunde
```

Apoi, din folderul `02-identitate-federata-keycloak`, în două terminale (sau ambele proiecte din Visual Studio ori Rider):

```bash
dotnet run --project src/Secretariat.Web
dotnet run --project src/Biblioteca.Web
```

**Resetarea completă** (utilizatori, sesiuni, emailuri, reimportul realm-ului): oprim ambele aplicații, rulăm `docker compose down -v` în `deploy`, ștergem `src/*/*.db` și pornim din nou.
Bazele de date ale aplicațiilor și Keycloak se resetează împreună, altfel registrul păstrează conturi Keycloak care nu mai există.

### Conturile personalului (create de importul realm-ului)

| Utilizatorul | Parola | Rolurile |
|---|---|---|
| `secretara` | `Secretara#2026` | `secretariat: secretary` |
| `bibliotecar` | `Bibliotecar#2026` | `biblioteca: librarian` |

Ambele conturi cer înregistrarea unei aplicații de autentificare la prima autentificare (acțiunea obligatorie `CONFIGURE_TOTP`).

## Scenariul laboratorului

1. **Consola este doar un client.** Deschidem consola de administrare Keycloak cu DevTools → Network. La clic pe *Users*, consola cere lista prin `GET /admin/realms/universitate/ui-ext/brute-force-user?...`, o adresă a aceluiași API REST de administrare.
2. **Înregistrarea studenților din Secretariat.** Ne autentificăm ca `secretara` → *Import CSV* cu fișierul exemplu (sau *Add student*). Pagina studentului afișează exact apelurile API-ului de administrare făcute de Secretariat. În consolă, utilizatorii apar cu atributele needitabile `student_id`, `enrollment_status`, `faculty`, iar *Realm settings → Events → Admin events* arată că i-a creat `service-account-secretariat`.
3. **Înrolarea.** În Mailpit deschidem linkul de invitație al unui student: Keycloak verifică emailul (linkul dovedește că adresa este a lui), apoi cere o aplicație de autentificare și o parolă. În lista studenților din Secretariat, *Email verified*, *Password* și *MFA* devin ✅.
4. **Împrumutul din catalog (versiunea A).** Într-o fereastră privată, ne autentificăm în Biblioteca drept student (parolă și cod de unică folosință) și împrumutăm o carte. Butonul *Token* (dreapta sus) arată că jetonul de identitate conține `enrollment_status: "enrolled"`.
5. **Problema.** Ca secretară, trecem studentul în *suspended* **fără** să bifăm încheierea sesiunilor. Studentul împrumută încă o carte: **împrumutul este acceptat**, iar mesajul arată vârsta jetonului pe care l-a crezut Biblioteca. În cel mult 2 minute aplicația își reîmprospătează jetoanele, catalogul afișează `suspended`, iar împrumutul este refuzat.
6. **Întrebarea pusă sursei autoritare (versiunea B).** Ne autentificăm în Biblioteca drept `bibliotecar` → *Lending desk* și introducem numărul matricol: Biblioteca obține un jeton pentru ea însăși (client credentials) și apelează `GET /api/students/{id}/enrollment` la Secretariat. Răspunsul este mereu actual, iar un student suspendat este refuzat imediat.
7. **Încheierea imediată a sesiunilor.** Trecem studentul înapoi în *enrolled*, de data aceasta cu bifa de încheiere a sesiunilor Keycloak. Keycloak apelează imediat `/backchannel-logout` în ambele aplicații, iar la următorul clic studentul este deconectat din Biblioteca; noua autentificare aduce valoarea nouă. Bonus: deconectarea din Secretariat îl deconectează și din Biblioteca (deconectarea unică).

## Cum este construit

```
deploy/
  docker-compose.yml                Keycloak 26.8.2, PostgreSQL 17, Mailpit
  keycloak/realm-universitate.json  exportul realm-ului, importat la prima pornire
src/
  Universitate.Shared/              comun ambelor aplicații
    KeycloakAuthenticationExtensions.cs   cookie + OIDC (cod de autorizare + PKCE), /account/login, /account/logout
    TokenRefresher.cs                     reîmprospătarea jetoanelor; deconectare când sesiunea Keycloak nu mai există
    BackChannelLogout.cs                  POST /backchannel-logout: Keycloak anunță încheierea unei sesiuni
    ServiceAccountToken.cs                jetonul contului de serviciu (client credentials), cu cache și handler HttpClient
    TokenClaims.cs                        afirmațiile din jetonul de identitate, verificarea rolurilor din resource_access
    Pages/Debug/Token.cshtml              inspectorul de jetoane
  Secretariat.Web/
    Keycloak/KeycloakAdminClient.cs       client scris de mână pentru API-ul REST de administrare (fără SDK, intenționat)
    Keycloak/StudentProvisioning.cs       întâi registrul, apoi Keycloak; erorile de sincronizare rămân vizibile
    Api/EnrollmentApi.cs                  API protejat cu jetoane purtător (audiență + rol de client)
    Pages/Students/*                      lista, adăugarea, importul CSV, editarea, suspendarea, încheierea sesiunilor
    Pages/Me.cshtml                       dosarul propriu al studentului
    wwwroot/samples/students.csv          cinci studenți exemplu
  Biblioteca.Web/
    Pages/Index.cshtml(.cs)               catalogul și împrumutul din catalog (versiunea A)
    Pages/Desk/Index.cshtml(.cs)          ghișeul de împrumut (versiunea B)
    Services/SecretariatApiClient.cs      apelează API-ul Secretariatului cu contul de serviciu al Bibliotecii
```

### Realm-ul `universitate` (ce configurează importul)

* Înregistrarea liberă este **oprită**. Linkurile de invitație sunt valabile 3 zile. Jetoanele de acces trăiesc **120 de secunde** (intenționat puțin, ca demonstrația prospețimii să fie rapidă).
* **Profilul utilizatorului:** `firstName`, `lastName`, `email`, `student_id`, `enrollment_status`, `faculty` pot fi modificate doar de administratori.
  Atributele *nedeclarate* aici sunt ignorate fără eroare de API-ul de administrare (`204 No Content`, nimic stocat).
* **Grupul `studenti`** acordă `secretariat: student` și `biblioteca: reader`.
* **Clienții** (`fullScopeAllowed` este oprit, deci jetoanele poartă doar rolurile din scope mappings ale fiecărui client):
  * `secretariat`: autentificare + cont de serviciu cu rolurile `realm-management` `manage-users`, `view-users`, `query-users`, `query-groups`.
    Din Keycloak 26.6, un jeton pentru API-ul de administrare are nevoie și de **scope-ul `kc-admin-api`**, care adaugă audiența API-ului. Aici este un scope *opțional*, cerut doar pentru jetonul contului de serviciu (`ServiceAccountScopes` în `appsettings.json`), deci jetoanele de autentificare ale utilizatorilor nu îl poartă. Fără el, API-ul de administrare răspunde 401, deși rolurile sunt corecte.
  * `biblioteca`: autentificare + cont de serviciu cu rolul `secretariat-api: enrollment.read`. Mapperul încorporat *audience resolve* adaugă `secretariat-api` în `aud`, pentru că jetonul poartă un rol al acestui client.
  * `secretariat-api`: fără autentificări; definește doar rolul `enrollment.read`.
* **URI-urile de redirecționare:** `/signin-oidc` (întoarcerea după autentificare) și `/` (unde ajunge linkul de invitație). Orice altă adresă este refuzată cu „Invalid redirect uri".
* **Adresa de deconectare pe canalul din spate:** `http://host.docker.internal:<port>/backchannel-logout`. Keycloak rulează în Docker, unde `localhost` este chiar containerul.
* **Mapper-ele** (pe fiecare client): rolurile clientului → afirmația simplă `roles`, ca să funcționeze `[Authorize(Roles = "...")]`; `student_id` (ambele aplicații); `enrollment_status` (doar Biblioteca).
* SMTP trimite în Mailpit. Evenimentele de autentificare și de administrare sunt activate.

## Probleme frecvente

| Simptomul | Cauza și soluția |
|---|---|
| orice cod este respins cu „Invalid authenticator code" | ceasul din Docker a rămas în urmă (frecvent după repausul laptopului): `wsl --shutdown`, repornim Docker Desktop, apoi `docker compose up -d` |
| un cod care tocmai a funcționat este respins | Keycloak nu acceptă același cod de două ori în fereastra lui de 30 de secunde; așteptăm codul următor |
| Secretariatul afișează „sync error … 401" la adăugarea studenților | jetonul contului de serviciu nu are scope-ul `kc-admin-api` (vezi mai sus) |
| „Invalid redirect uri" la trimiterea invitațiilor | `KeycloakAdmin:InvitationRedirectUri` nu se află printre URI-urile de redirecționare ale clientului `secretariat` |
| încheierea sesiunilor deconectează studentul abia după aproximativ 2 minute | Keycloak nu ajunge la `host.docker.internal:<port>`; verificăm portul sau firewall-ul |
| registrul afișează „missing in Keycloak" | Keycloak a fost resetat fără fișierele `.db` (sau utilizatorul a fost șters din consolă); le resetăm împreună |

## Simplificări de laborator (nu se copiază în producție)

* HTTP peste tot (`RequireHttpsMetadata = false`, `start-dev`).
* Secretele clienților și parolele personalului stau în fișierele din depozit. În producție: User Secrets sau un seif de secrete.
* Administratorul Keycloak este `admin` / `admin`.
* Sesiunile încheiate sunt ținute minte în memorie, separat de fiecare instanță a aplicației. Cu mai multe instanțe ar fi nevoie de un cache comun.

Exercițiile laboratorului se află în paginile cursului, la finalul fiecărei secțiuni.
