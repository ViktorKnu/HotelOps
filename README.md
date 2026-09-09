# HotelOps

HotelOps skal bli en felles plattform for hotellgjester og ansatte. Prosjektet
bygges inkrementelt med hovedvekt på realistiske arbeidsflyter mellom resepsjon,
renhold, vedlikehold og gjester.

## Status

Prosjektet er i en tidlig fase. Grunnstrukturen for backend og frontend er på
plass, og den første domenemodellen for hotellrom er implementert.

Et rom har separate statuser for belegg, rengjøring og drift. Det regnes bare
som klart for innsjekking når det er ledig, rent og operativt.

Domenemodellen håndhever rengjøringsflyten
`Ren → Skitten → Under rengjøring → Ren`. Ugyldige hopp og gjentakelser blir
avvist. Flyten kan styres gjennom API-et. Rengjøringsoppgaver opprettes ikke ennå.

EF Core-oppsett og en første PostgreSQL-migrering for rom er på plass.
`GET /api/rom` henter romoversikten fra databasen, og `POST /api/rom` registrerer
nye rom. Egne endepunkter utfører overgangene i rengjøringsflyten. Frontend viser
romoversikten med separate statuser og nøkkeltall, har et enkelt skjema for å
registrere rom og viser neste gyldige rengjøringshandling på hvert romkort.
Romoversikten kan filtreres på romnummer, etasje og driftsbehov (klare for
innsjekking, krever renhold eller driftsavvik). Filtrene kan kombineres og
nullstilles. Nøkkeltallene viser alltid hele hotellet, mens treffantallet viser
utvalget. Utvalget oppdateres også når rengjøringsstatus endres.

En egen renholdstavle viser skitne rom og rom under rengjøring i hver sin
kolonne. Tavlen kan filtreres på etasje og viser hasteoppgaver først, deretter
etasje og romnummer i naturlig tallrekkefølge. Start og fullfør renhold direkte fra
romkortene; kolonner og nøkkeltall oppdateres etter vellykket lagring.
Rene rom vises ikke på tavlen. Ansvarlig renholder og prioritet kan lagres på
hvert rom via tavlen. Ansvarlig er et fritt navnefelt, ikke en kobling til et
ansattregister. Tavlen bruker eksisterende romstatus uten egne oppgaveposter
eller historikk. Tildeling og prioritet nullstilles når rengjøringen fullføres.

Renholdstavlen kan også filtreres på ansvarlig renholder, «Ikke tildelt» og
prioritet. Filtrene kombineres med etasje og kan nullstilles samlet.
Treffantall og kolonner gjelder utvalget; nøkkeltallene øverst gjelder hele
hotellet. Et valgt navn beholdes i filteret når siste rom fullføres eller
tildeles på nytt, slik at tavlen viser et tomt utvalg fremfor andre ansattes
rom. Navnene hentes fra rommenes tildelinger, ikke fra et ansattregister.

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
tester/                     xUnit-tester av domenereglene og rom-API-et
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

API-testene bruker ASP.NET Cores
[WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-9.0)
med en testleser i stedet for PostgreSQL. De kontrollerer HTTP-svar, DTO-mapping,
tekstbaserte statuser og feilhåndtering, men verifiserer ikke databaselesing
eller databasesortering.

Start frontend i en annen terminal:

```bash
cd frontend
npm install
npm run dev
```

Vites utviklingsserver videresender kall til `/api` til API-et på
`http://localhost:5091`. Begge applikasjonene må derfor kjøre samtidig for å
vise romdata. Frontend håndterer også laste-, tom- og feiltilstand.
Registreringsskjemaet viser validerings- og konfliktmeldinger fra API-et og
oppdaterer oversikten etter et vellykket kall.

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

## Romoversikt i API-et

`GET /api/rom` returnerer `200 OK` med en JSON-liste, sortert på romnummer som
tekst i databasen. En tom database gir `[]`. Databasen må være tilgjengelig og
migrert før dette endepunktet kan brukes; det legges ikke inn demonstrasjonsrom
automatisk.

Eksempel på respons når et rom finnes:

```json
[
  {
    "id": "f1f34e19-baf0-4b1c-af6d-a0b3cd1da8bd",
    "nummer": "101",
    "etasje": 1,
    "beleggsstatus": "Ledig",
    "rengjøringsstatus": "Ren",
    "driftsstatus": "Operativ",
    "erKlartForInnsjekking": true,
    "ansvarligRenholder": null,
    "renholdsprioritet": "Normal",
    "versjon": "29cc73b4-37a4-49a1-9087-f309cfed7c4f"
  }
]
```

Klarstatus beskriver rommets nåværende tilstand, ikke tilgjengelighet for en
bestemt reservasjonsperiode. Uventede lesefeil gir `500 Internal Server Error`
som `application/problem+json`, med en norsk melding uten interne feildetaljer.

Eksempelkall ligger i `backend/HotelOps.Api/HotelOps.Api.http`. OpenAPI-dokumentet
er tilgjengelig på `/openapi/v1.json` i utviklingsmiljøet.

## Registrere rom i API-et

`POST /api/rom` registrerer ett rom med standardstatusene `Ledig`, `Ren` og
`Operativ`. Forespørselen inneholder bare romnummer og etasje:

```json
{
  "nummer": "101",
  "etasje": 1
}
```

Et vellykket kall gir `201 Created` og det opprettede rommet i samme format som
romoversikten. Tomt romnummer gir `400 Bad Request`. Romnummeret trimmes, og et
nummer som allerede finnes gir `409 Conflict`. Den unike databaseindeksen avgjør
duplikatkonflikten og beskytter også mot samtidige registreringsforsøk.

## Endre rengjøringsstatus i API-et

Rengjøringsflyten styres med tre handlinger:

```text
PATCH /api/rom/{romId}/rengjoring/marker-skitten
PATCH /api/rom/{romId}/rengjoring/start
PATCH /api/rom/{romId}/rengjoring/fullfor
```

Et vellykket kall gir `200 OK` med oppdatert rom. Ukjent rom gir `404 Not Found`,
mens en overgang som bryter statusrekkefølgen gir `409 Conflict`. Endringen
lagres først etter at domenemodellen har godkjent overgangen.

Autentisering og rollebasert tilgang er ikke implementert. Romoversikten er
foreløpig ubeskyttet, og endepunktene for registrering og rengjøring er også
åpne. Prosjektet er kun ment for lokal utvikling, ikke for offentlig bruk med
reelle hotelldata.

## Planlegge renhold

`PATCH /api/rom/{romId}/rengjoring/planlegg` lagrer ansvarlig og prioritet
for et skittent rom eller et rom under rengjøring:

```json
{
  "ansvarligRenholder": "Kari",
  "prioritet": "Haster"
}
```

Prioritet må være `Normal` eller `Haster`. Navnet trimmes og kan ha maksimalt
100 tegn. Null eller blankt navn fjerner tildelingen. Begge verdiene erstattes
ved lagring. Endepunktet gir oppdatert rom ved `200 OK`, `400` ved ugyldige
felt, `404` for ukjent rom og `409` for et rent rom. Endepunktet er ubeskyttet
på samme måte som de øvrige romendepunktene.

Migreringen `LeggTilRenholdsplanlegging` legger til feltene. Eksisterende rom
får normal prioritet og ingen ansvarlig. Kjør migreringen før oppdatert API:

```bash
dotnet ef database update --project backend/HotelOps.Infrastruktur --startup-project backend/HotelOps.Api -- --environment Development
```

## Samtidige endringer

Ved lagring kontrollerer EF Core at rommets status, ansvarlig renholder og
prioritet fortsatt samsvarer med verdiene lest av API-forespørselen. Hvis en
annen forespørsel har endret disse i mellomtiden, avvises lagringen med
`409 Conflict` og en melding om å oppdatere oversikten. Endringen forsøkes
ikke automatisk på nytt. Knappen «Oppdater oversikten» henter siste romdata.

Alle romsvar inneholder også `versjon`. Hver gyldige renholdsendring gir en
ny versjon. Alle fire PATCH-endepunkter krever headeren `X-Rom-Versjon` med
versjonen klienten hentet, som UUID med bindestreker uten anførselstegn:

```http
X-Rom-Versjon: 29cc73b4-37a4-49a1-9087-f309cfed7c4f
```

Manglende eller ugyldig versjon gir `400 Bad Request`. En utdatert versjon
gir `409 Conflict` før rommet endres. Frontend sender versjonen automatisk
og bruker ny versjon fra et vellykket svar. Dermed oppdages også endringer
som var lagret før forespørselen startet. Oppdatering av
oversikten lukker åpne renholdsplaner og forkaster ulagrede skjemaendringer.

Migreringen `KontrollerSamtidigeRomendringer` registrerer endringen i
EF-modellen uten å endre tabellkolonnene. Testene simulerer lagringskonflikt
og kontrollerer HTTP-svar; samtidige transaksjoner mot PostgreSQL er ikke
dekket av disse testene.

Migreringen `LeggTilRomversjon` legger til versjonskolonnen og må kjøres før
oppdatert API tas i bruk, med `dotnet ef database update`-kommandoen over.
Eksisterende rom får null-UUID som startversjon og en ny UUID ved første
endring. Oppdater også eventuelle API-klienter: PATCH uten versjonsheader
godtas ikke lenger.

## Videre utvikling

Funksjonalitet og dokumentasjon utvides i små, avgrensede steg. README-en skal
kun beskrive funksjonalitet som faktisk finnes i prosjektet.
