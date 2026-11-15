# Platforma de cursuri (Cursuri)

Platforma de cursuri online a universității de laborator, a treia aplicație alături de Secretariat și Biblioteca din [`../../02-identitate-federata-keycloak/`](../../02-identitate-federata-keycloak/). Este o aplicație ASP.NET Core MVC (.NET 10), cu datele ținute în memorie și cu autentificare locală: studenții și profesorii intră cu un nume de utilizator și o parolă păstrate (ca hash) de aplicație. Aspectul vine din foaia de stil comună `universitate.css`, cu tema `app-cursuri`.

Este starea finală a exercițiilor 1-14 din cursul **Tehnologii pentru Asigurarea Securității și Confidențialității Datelor**: fiecare exercițiu corespunde unui pas (un commit), de la proiectul gol până la pagina „Who am I".

| Exercițiile | Ce adaugă |
|---|---|
| 1-3 | proiectul, tema universității, log-ul cererilor (middleware) |
| 4-5 | catalogul cursurilor (`/Courses`) și serviciul `ICourseCatalog` |
| 6-9 | modelul identității, registrul platformei (`/People`), registrul simulat al secretariatului (`/Secretariat`), aceeași persoană în două contexte |
| 10 | căutarea după numele de utilizator (identificarea) |
| 11-14 | formularul de autentificare, parola verificată cu `PasswordHasher`, cookie-ul de autentificare, pagina „Who am I" (`/Account/Me`) și sign-out-ul |

## Pornire

Cerințe: SDK-ul .NET 10 și certificatul de dezvoltare HTTPS (`dotnet dev-certs https --trust`). Din acest folder:

```bash
dotnet run --project Cursuri --launch-profile https
```

Aplicația ascultă pe https://localhost:7101 (și pe http://localhost:5101, redirecționat spre HTTPS). Pentru lucru, `dotnet watch --project Cursuri` o repornește la fiecare fișier salvat.

## Conturile de test

Toate conturile au parola `Parola.2026`; aplicația îi calculează hash-ul la pornire.

| Utilizator | Nume | Rol | Matricol |
|---|---|---|---|
| `ana.popescu` | Ana Popescu | student | `S2026001` |
| `mihai.dumitrescu` | Mihai Dumitrescu | student | `S2026002` |
| `elena.dumitru` | Elena Dumitru | student | `S2024017` (înmatricularea a expirat) |
| `andreea.vasile` | Andreea Vasile | profesor | - |
| `victor.neagu` | Victor Neagu | profesor | - |

Secretariatul este simulat în aceeași aplicație (`InMemorySecretariatRegistry`); toate datele, inclusiv CNP-urile, sunt inventate. Platforma nu are încă autorizare: paginile People și Secretariat sunt deschise oricui, intenționat, ca punct de plecare pentru discuția din curs.
