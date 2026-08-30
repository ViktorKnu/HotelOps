# HotelOps

HotelOps skal bli en felles plattform for hotellgjester og ansatte. Prosjektet
bygges inkrementelt med hovedvekt på realistiske arbeidsflyter mellom resepsjon,
renhold, vedlikehold og gjester.

## Status

Prosjektet er i en tidlig fase. Grunnstrukturen for backend og frontend er på
plass, men domenefunksjonalitet er ennå ikke implementert.

## Teknologistack

- ASP.NET Core Web API og C#
- React, TypeScript og Vite
- PostgreSQL og Entity Framework Core planlegges når datalagring introduseres

## Prosjektstruktur

```text
backend/
  HotelOps.Domene/          Domenemodell og forretningsregler
  HotelOps.Applikasjon/     Brukstilfeller og applikasjonslogikk
  HotelOps.Infrastruktur/   Eksterne tjenester og datalagring
  HotelOps.Api/             HTTP-endepunkter og konfigurasjon
frontend/                   React-applikasjon
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

Start frontend i en annen terminal:

```bash
cd frontend
npm install
npm run dev
```

## Videre utvikling

Funksjonalitet og dokumentasjon utvides i små, avgrensede steg. README-en skal
kun beskrive funksjonalitet som faktisk finnes i prosjektet.
