# TASCD: laboratoarele de identitate digitală

Codul laboratorului cursului **Tehnologii pentru Asigurarea Securității și Confidențialității Datelor** de pe [docs.lucianvilcea.ro](https://docs.lucianvilcea.ro), despre identitatea digitală. Repository-ul conține cele două etape ale laboratorului, fiecare în folderul ei, cu propria soluție .NET.

| Folderul | Ce conține | Săptămânile |
|---|---|---|
| [`01-autentificare-locala/`](01-autentificare-locala/) | aplicația Biblioteca (ASP.NET Core MVC), cu conturi, parole și linkuri prin email gestionate chiar de aplicație, peste EF Core, în două variante (implementare proprie și ASP.NET Core Identity), plus serviciul web al Secretariatului | 1-2: implementarea de referință pentru exercițiile 1-14 |
| [`02-identitate-federata-keycloak/`](02-identitate-federata-keycloak/) | aplicațiile Secretariat și Biblioteca (Razor Pages), care își deleagă autentificarea lui Keycloak, plus infrastructura în Docker | 2-3: autentificarea federată și autorizarea prin claim-uri |

Cele două etape spun aceeași poveste: o bibliotecă universitară care, în prima etapă, își verifică singură utilizatorii și, în a doua, renunță la parolele proprii în favoarea unui identity provider comun.

## Cerințe

- SDK-ul .NET 10
- Git
- Docker Desktop (serverul de email de test al ambelor laboratoare și, în al doilea, Keycloak)
- pentru al doilea laborator: o aplicație de autentificare (de exemplu Google Authenticator, Microsoft Authenticator sau FreeOTP)

## Pornire rapidă

```bash
git clone https://github.com/lucianvilcea26/TASCD.git
cd TASCD
```

Apoi urmăm instrucțiunile din fișierul `README.md` al laboratorului dorit.

## Ramurile `exercise-NN`

Ramurile `exercise-01` … `exercise-27` păstrează starea codului după fiecare exercițiu din seminarul cursului de Tehnologii pentru Asigurarea Securității și Confidențialității Datelor, în structura de dinainte de împărțirea pe foldere: proiectele `Biblioteca` și `Secretariat` se află acolo direct în rădăcina depozitului.

## Notă

Toate datele, persoanele și conturile din laboratoare sunt fictive. Parolele, secretele și cheile din fișierele de configurare sunt valori de laborator, publice prin definiție; nu se refolosesc nicăieri altundeva.
