# Platforma de cursuri (Cursuri)

Platforma de cursuri online a universității de laborator, a treia aplicație alături de Secretariat și Biblioteca din [`../../02-identitate-federata-keycloak/`](../../02-identitate-federata-keycloak/). Este o aplicație ASP.NET Core MVC (.NET 10), cu datele ținute în memorie și cu autentificare locală: studenții, profesorii și administratorul intră cu un nume de utilizator și o parolă păstrate (ca hash) de aplicație, iar oricine își poate crea singur un cont. Aspectul vine din foaia de stil comună `universitate.css`, cu tema `app-cursuri`.

Este starea finală a exercițiilor 1-16 din cursul **Tehnologii pentru Asigurarea Securității și Confidențialității Datelor**: fiecare exercițiu corespunde unui pas (un commit), de la proiectul gol până la administrarea utilizatorilor.

| Exercițiile | Ce adaugă |
|---|---|
| 1-3 | proiectul, tema universității, log-ul cererilor (middleware) |
| 4-5 | catalogul cursurilor (`/Courses`) și serviciul `ICourseCatalog` |
| 6-9 | modelul identității, registrul platformei (`/People`), registrul simulat al secretariatului (`/Secretariat`), aceeași persoană în două contexte |
| 10 | căutarea după numele de utilizator (identificarea) |
| 11-14 | formularul de autentificare, parola verificată cu `PasswordHasher`, cookie-ul de autentificare, pagina „Who am I" (`/Account/Me`), sign-out-ul și paginile care cer autentificare |
| 15 | înregistrarea (`/Account/Register`): conturi create de utilizatori, cu numele și matricolul declarate de ei (issuer `Self-asserted`) |
| 16 | rolurile `student`, `profesor`, `administrator` și pagina „Users" (`/Admin/Users`, doar pentru administratori): schimbarea rolului, resetarea parolei, ștergerea contului și verificarea cu registrul secretariatului |

## Pornire

Cerințe: SDK-ul .NET 10 și certificatul de dezvoltare HTTPS (`dotnet dev-certs https --trust`). Din acest folder:

```bash
dotnet run --project Cursuri --launch-profile https
```

Aplicația ascultă pe https://localhost:7101 (și pe http://localhost:5101, redirecționat spre HTTPS). Pentru lucru, `dotnet watch --project Cursuri` o repornește la fiecare fișier salvat.

## Conturile de test

Conturile create la pornire au parola `Parola.2026`; aplicația îi calculează hash-ul la pornire. Excepție face administratorul, `ioana.marin`: parola lui vine din configurare, din cheia `Platform:AdminPassword` (de exemplu, variabila de mediu `Platform__AdminPassword`), și este `Parola.2026` doar dacă cheia lipsește. Pe un server public, cheia trebuie setată, ca administratorul să nu aibă parola publicată în curs.

| Utilizator | Nume | Rol | Matricol |
|---|---|---|---|
| `ana.popescu` | Ana Popescu | student | `S2026001` |
| `mihai.dumitrescu` | Mihai Dumitrescu | student | `S2026002` |
| `elena.dumitru` | Elena Dumitru | student | `S2024017` (înmatricularea a expirat) |
| `andreea.vasile` | Andreea Vasile | profesor | - |
| `victor.neagu` | Victor Neagu | profesor | - |
| `ioana.marin` | Ioana Marin | administrator | - |

Pe lângă acestea, oricine își poate crea un cont din „Register", cu o parolă de cel puțin 15 caractere; contul primește rolul `student`. Conturile înregistrate trăiesc în memorie și dispar la repornirea aplicației, iar registrul acceptă cel mult 500 de conturi.

Secretariatul este simulat în aceeași aplicație (`InMemorySecretariatRegistry`); toate datele, inclusiv CNP-urile, sunt inventate. Paginile Courses, People și Secretariat cer doar un utilizator autentificat, oricare ar fi rolul lui: Ana, studentă, vede și ea registrul secretariatului, intenționat, ca punct de plecare pentru discuția despre autorizare din curs. Singura pagină protejată pe rol este „Users", cu `[Authorize(Roles = "administrator")]`.
