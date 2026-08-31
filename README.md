# HotelOps

HotelOps skal bli en felles plattform for hotellgjester og ansatte. Prosjektet
bygges inkrementelt med hovedvekt på realistiske arbeidsflyter mellom resepsjon,
renhold, vedlikehold og gjester.

## Status

Prosjektet er i en tidlig fase. Grunnstrukturen for backend og frontend er på
plass, og den første domenemodellen for hotellrom er implementert.

Et rom har separate statuser for belegg, rengjøring og drift. Det regnes bare
som klart for innsjekking når det er ledig, rent og operativt.

EF Core-oppsett og en første PostgreSQL-migrering for rom er på plass. Det finnes
ennå ikke API-endepunkter for å registrere eller hente rom.

## Teknologistack

- ASP.NET Core Web API og C#
- React, TypeScript og Vite
- PostgreSQL og Entity Framework Core med Npgsql

## Prosjektstruktur

```text
backend/
  HotelOps.Domene/          Domenemodell og forretningsregler
  HotelOps.Applikasjon/     Brukstilfeller og applikasjonslogikk
  HotelOps.Infrastruktur/   Eksterne tjenester og datalagring
  HotelOps.Api/             HTTP-endepunkter og konfigurasjon
frontend/                   React-applikasjon
tester/                     xUnit-tester av domenereglene
```

Backend er organisert som en modulær monolitt. Avhengighetene peker innover mot
domenelaget, mens frontend er en separat applikasjon som etter hvert kommuniserer
med API-et.

## Lokal utvikling

Forutsetninger:

- .NET SDK 9
- Node.js 20 eller nyere

Start backend:

```bash
dotnet run --project backend/HotelOps.Api
```

API-ets enkle helsesjekk er tilgjengelig på `GET /api/helse`.
Den sjekker bare at API-et svarer, ikke databasetilkoblingen.

Kjør backendtestene:

```bash
dotnet test
```

Start frontend i en annen terminal:

```bash
cd frontend
npm install
npm run dev
```

## Database

Rom lagres i tabellen `Rom` med ID, romnummer, etasje og de tre statusene.
Romnummer har en unik indeks; foreløpig gjelder modellen ett hotell.
Statusene lagres som tekst. `ErKlartForInnsjekking` beregnes av domenemodellen
og lagres ikke som en egen kolonne.

Opprett først en tom PostgreSQL-database og en bruker som eier databasen.
Standardinnstillingene for utvikling bruker `localhost:5432`, databasen
`hotelops` og brukeren `hotelops`. Passord er ikke lagret i repositoryet.

Sett hele tilkoblingsstrengen i terminalen du bruker til migrering og API-kjøring.
Eksempel i PowerShell (erstatt plassholderen med ditt lokale passord):

```powershell
$env:ConnectionStrings__Hotell = "Host=localhost;Port=5432;Database=hotelops;Username=hotelops;Password=<ditt-lokale-passord>"
```

Ikke legg virkelige passord i `appsettings`-filer eller Git. Utenfor
utviklingsmiljøet må tilkoblingsstrengen konfigureres separat.

Kjør fra prosjektroten for å installere prosjektets versjonslåste EF-verktøy og
opprette tabellen gjennom migreringen:

```bash
dotnet tool restore
dotnet ef database update --project backend/HotelOps.Infrastruktur --startup-project backend/HotelOps.Api -- --environment Development
```

Migreringer kjøres eksplisitt, ikke automatisk ved API-oppstart. De ligger i
`backend/HotelOps.Infrastruktur/Datalagring/Migreringer`.

SQL-en kan også genereres og gjennomgås uten en kjørende database:

```bash
dotnet ef migrations script --project backend/HotelOps.Infrastruktur --startup-project backend/HotelOps.Api -- --environment Development
```

Se [Npgsql-dokumentasjonen](https://www.npgsql.org/efcore/) og
[EF Core-verktøyene](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
for mer om provideroppsett og migreringskommandoer.

## Videre utvikling

Funksjonalitet og dokumentasjon utvides i små, avgrensede steg. README-en skal
kun beskrive funksjonalitet som faktisk finnes i prosjektet.
