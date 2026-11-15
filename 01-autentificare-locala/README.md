# Laboratorul 1: autentificarea locală, cu nume de utilizator și parolă

Biblioteca universitară din cursul **Tehnologii pentru Asigurarea Securității și Confidențialității Datelor**, cu conturile păstrate chiar de aplicație, într-o bază SQLite administrată prin Entity Framework Core. Aceeași aplicație există în **două variante**, cu aceleași pagini și aceleași reguli, ca să poată fi comparate:

| Folderul | Cum sunt construite conturile | Soluția |
|---|---|---|
| [`implementare-proprie/`](implementare-proprie/) | de mână: entitatea `User`, `PasswordHasher<T>` pentru parole, `AccountService` cu toate regulile, linkuri păstrate ca hash-uri SHA-256 | `ImplementareProprie.slnx` |
| [`aspnet-identity/`](aspnet-identity/) | cu ASP.NET Core Identity: `IdentityUser`, `UserManager`, `SignInManager`, token-uri `DataProtectorTokenProvider` | `AspNetIdentity.slnx` |

Ambele folosesc același serviciu web al secretariatului, din [`Secretariat/`](Secretariat/), și același server de email de laborator.

Folderul [`platforma-cursuri/`](platforma-cursuri/) conține aplicația construită pas cu pas în exercițiile 1-14 ale cursului: platforma de cursuri Cursuri (MVC, conturi în memorie, parolă verificată cu `PasswordHasher`, autentificare cu cookie), cu soluția ei, `TASCD.slnx`, și propriul [README](platforma-cursuri/README.md).

## Fluxurile

| Fluxul | Ce se întâmplă |
|---|---|
| Crearea contului | studentul introduce numărul matricol, adresa de email și o parolă; biblioteca întreabă secretariatul dacă adresa este cea din dosar și dacă studentul este înmatriculat; dacă da, creează contul **neconfirmat** și trimite un link de activare la adresa din dosar |
| Confirmarea emailului | linkul (valabil o oră, o singură dată) activează contul; până atunci, autentificarea este refuzată |
| Autentificarea | cu adresa de email și parola; același mesaj pentru email inexistent și parolă greșită |
| Blocarea contului | după 5 parole greșite la rând, contul este blocat 5 minute, chiar și pentru parola corectă |
| Ieșirea | închide sesiunea (cookie-ul) |
| Schimbarea parolei | cere parola curentă; schimbă security stamp-ul, deci celelalte sesiuni ale contului se închid la următoarea cerere; titularul primește un email de notificare |
| Parola uitată | un link prin email (valabil o oră, o singură dată) pentru alegerea unei parole noi; răspunsul paginii este același pentru adrese cu și fără cont; resetarea închide sesiunile deschise cu parola veche |

Regulile pentru parole: cel puțin 10 caractere, cel mult 128, fără adresa de email în ea și fără reguli de compoziție (majuscule, cifre, simboluri), cum recomandă NIST SP 800-63B. Laboratorul rămâne sub revizia 4 a ghidului, care cere cel puțin 15 caractere când parola este singurul factor și verificarea parolelor noi într-o listă de parole compromise. Biblioteca păstrează celelalte funcții: catalogul, împrumutul cu politica `CanBorrow` (înmatriculat acum, după secretariat, și cel mult 3 cărți) și fișele cititorilor, pentru bibliotecar.

## Pornire

Cerințe: SDK-ul .NET 10, Docker Desktop (doar pentru serverul de email) și certificatul de dezvoltare HTTPS (`dotnet dev-certs https --trust`).

1. Serverul de email de laborator, din folderul `deploy`:

   ```bash
   docker compose up -d
   ```

   Emailurile trimise de bibliotecă apar la http://localhost:8026.

2. Cheia de API dintre bibliotecă și secretariat, o singură dată, din acest folder (înlocuim `CHEIA` cu un șir lung și aleator):

   ```bash
   dotnet user-secrets set ApiKey "CHEIA" --project Secretariat
   dotnet user-secrets set Secretariat:ApiKey "CHEIA" --project implementare-proprie/Biblioteca
   dotnet user-secrets set Secretariat:ApiKey "CHEIA" --project aspnet-identity/Biblioteca
   ```

3. Secretariatul și una dintre variante (sau ambele, au porturi diferite), în terminale separate:

   ```bash
   dotnet run --project Secretariat --launch-profile https
   dotnet run --project implementare-proprie/Biblioteca --launch-profile https
   dotnet run --project aspnet-identity/Biblioteca --launch-profile https
   ```

| Aplicația | Adresa |
|---|---|
| Secretariat | https://localhost:7102 |
| Biblioteca, implementare proprie | https://localhost:7101 |
| Biblioteca, ASP.NET Core Identity | https://localhost:7103 |
| Mailpit | http://localhost:8026 |

La prima pornire, fiecare variantă își creează baza `biblioteca.db` aplicând migrările EF Core și adaugă contul bibliotecarului. **Resetarea** unei variante: o oprim și ștergem fișierul `biblioteca.db` din folderul ei.

### Conturile

| Cine | Cum intră |
|---|---|
| bibliotecara Ioana Marin | `bibliotecar@biblioteca.ase.local`, parola `Raftul.de.carti.2026` (creat la pornire) |
| Ana Popescu, `SIM-2026-0412` | își creează singură contul cu `ana.popescu@student.ase.local` |
| Mihai Ionescu, `SIM-2026-0388` | își creează singur contul cu `mihai.ionescu@student.ase.local` |
| Elena Dumitru, `SIM-2024-0107` | nu își poate crea cont: înmatricularea ei a expirat |

### Migrările

Modelul fiecărei variante este în `Data/`, iar migrările în `Data/Migrations/`. După o modificare a modelului, din folderul variantei:

```bash
dotnet ef migrations add NumeleModificarii --output-dir Data/Migrations
```

Aplicația aplică migrările singură la pornire (`Database.MigrateAsync()` în `Data/Seed.cs`).

## Unde se află fiecare regulă

| Regula | Implementare proprie | ASP.NET Core Identity |
|---|---|---|
| hash-ul parolei | `AccountService`, cu `PasswordHasher<User>` | `UserManager.CreateAsync`, același `PasswordHasher` |
| politica parolei | `AccountService.CheckPassword` | `PasswordOptions` în `Program.cs` și `EmailNotInPasswordValidator` |
| blocarea | `AccountService.LoginAsync` (`FailedLoginCount`, `LockoutEndUtc`) | `LockoutOptions` și `CheckPasswordSignInAsync(..., lockoutOnFailure: true)` |
| emailul confirmat | `AccountService.LoginAsync` | `SignInOptions.RequireConfirmedEmail` |
| linkurile din email | tabelul `UserTokens`: valoare aleatoare de 256 de biți, în bază doar hash-ul SHA-256 | `DataProtectorTokenProvider`: date protejate criptografic, nimic în bază |
| închiderea celorlalte sesiuni | security stamp-ul din cookie, comparat la fiecare cerere în `UserPrincipal.ValidateStampAsync` | `SecurityStampValidator`, cu `ValidationInterval` pus pe zero |
| claim-urile sesiunii | `UserPrincipal.Create` | `BibliotecaClaimsFactory` |
| mesajele în română | scrise direct în cod | `RomanianIdentityErrorDescriber` |

## Diferențe observate între variante

Aceleași teste automate trec pe ambele variante, dar Identity se comportă diferit în câteva puncte, unele corectate în cod, altele lăsate intenționat vizibile:

| Situația | Implementare proprie | ASP.NET Core Identity |
|---|---|---|
| parolă **greșită** pentru un cont neconfirmat | „adresa de email sau parola nu sunt corecte": parola se verifică prima | „confirmă întâi adresa": Identity verifică emailul confirmat înaintea parolei, deci mesajul spune că există un cont |
| un link de confirmare folosit a doua oară | refuzat: linkul este marcat ca folosit | ar fi acceptat din nou până la expirare; codul tratează un cont deja confirmat ca pe un link folosit |
| un link nou de confirmare | anulează linkurile trimise anterior | linkurile vechi rămân valabile până la expirare |
| un email inexistent la autentificare | o parolă este totuși verificată, ca durata răspunsului să nu spună cine are cont | Identity nu face asta singur; codul o face explicit |
| blocarea după resetarea parolei | ridicată | Identity nu o ridică singur; codul o face explicit |
| verificarea ștampilei | la fiecare cerere | implicit la 30 de minute; în laborator la fiecare cerere |

## Simplificări de laborator

- Parola bibliotecarului stă în `appsettings.json`; în producție vine din User Secrets sau dintr-un seif de secrete.
- Emailurile pleacă prin SMTP fără criptare, către Mailpit; un server real cere TLS și autentificare.
- Ștampila se verifică la fiecare cerere, cu o interogare a bazei; cu mulți utilizatori, un interval de câteva minute este un compromis obișnuit.
- Nu există limitare a numărului de cereri pe adresă IP; blocarea pe cont oprește ghicirea parolei unui cont, nu încercarea aceleiași parole pe multe conturi.

Starea codului de la finalul fiecărui exercițiu din forma anterioară a laboratorului este păstrată în branch-urile `exercise-01` … `exercise-27`. Laboratorul următor, din folderul [`../02-identitate-federata-keycloak/`](../02-identitate-federata-keycloak/), mută autentificarea la un identity provider comun, Keycloak.
