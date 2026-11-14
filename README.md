# TASCD: laboratoarele de identitate digitală

Codul laboratoarelor pentru cursurile de pe [docs.lucianvilcea.ro](https://docs.lucianvilcea.ro) despre identitatea digitală. Depozitul conține două laboratoare independente, fiecare în folderul lui, cu propria soluție .NET.

| Folderul | Ce conține | Cursul |
|---|---|---|
| [`01-autentificare-locala/`](01-autentificare-locala/) | aplicația Biblioteca (ASP.NET Core MVC), cu conturi, parole și linkuri prin email gestionate chiar de aplicație, peste EF Core, în două variante (implementare proprie și ASP.NET Core Identity), plus serviciul web al Secretariatului | Tehnologii pentru Asigurarea Securității și Confidențialității Datelor |
| [`02-identitate-federata-keycloak/`](02-identitate-federata-keycloak/) | aplicațiile Secretariat și Biblioteca (Razor Pages), care își deleagă autentificarea lui Keycloak, plus infrastructura în Docker | Identitate federată cu Keycloak |

Cele două laboratoare spun aceeași poveste în două etape: o bibliotecă universitară care, în primul laborator, își verifică singură utilizatorii și, în al doilea, renunță la parolele proprii în favoarea unui furnizor de identitate comun.

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
