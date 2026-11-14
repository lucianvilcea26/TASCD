# Laboratorul 1: autentificarea locală

Aplicația unei biblioteci universitare și serviciul web al secretariatului, construite exercițiu cu exercițiu în seminarul cursului **Tehnologii pentru Asigurarea Securității și Confidențialității Datelor**. Aplicația își autentifică singură utilizatorii, fără un furnizor de identitate extern.

| Proiectul | Ce face | Adresa |
|---|---|---|
| `Biblioteca` | ASP.NET Core MVC: catalogul de cărți, cititorii, autentificarea cu cookie, împrumutul cu politica `CanBorrow` | `https://localhost:7101` |
| `Secretariat` | API web care spune dacă un student este înmatriculat, protejat cu o cheie de API | `https://localhost:7102` |

## Pornire

Secretariatul refuză să pornească fără o cheie de API, iar Biblioteca trebuie să trimită aceeași cheie. Cheia se păstrează cu User Secrets, în afara depozitului. O singură dată, din acest folder (înlocuim `CHEIA` cu un șir lung și aleator):

```bash
dotnet user-secrets set ApiKey "CHEIA" --project Secretariat
dotnet user-secrets set Secretariat:ApiKey "CHEIA" --project Biblioteca
```

Apoi, în două terminale:

```bash
dotnet run --project Secretariat --launch-profile https
dotnet run --project Biblioteca --launch-profile https
```

## Ce urmează

Laboratorul va fi rescris pentru a construi, pas cu pas, fluxuri simple de autentificare cu nume de utilizator și parolă, cu .NET și Entity Framework Core: înregistrarea, stocarea parolelor, autentificarea și sesiunea. Starea de la finalul fiecărui exercițiu din forma actuală este păstrată în ramurile `exercise-01` … `exercise-27`.

Laboratorul următor, din folderul [`../02-identitate-federata-keycloak/`](../02-identitate-federata-keycloak/), mută autentificarea la un furnizor de identitate comun, Keycloak.
