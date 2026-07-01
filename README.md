# Anime Tracker — Firefox Extension + Azure Microservices Platform

A Firefox browser extension that tracks daily anime releases, notifies users when episodes from their followed series air, and opens the right streaming page at the right episode — automatically.

Built as a distributed system on Azure with independently deployable services, a fully automated CI/CD pipeline, and a modular streaming provider architecture that makes adding new sites a one-file change.

## Screenshots

**Database schema**

[![Database schema](https://drive.google.com/uc?export=view&id=1UITmOekanWsNpEodd3le-v77nmxQX3W4)](https://drive.google.com/file/d/1UITmOekanWsNpEodd3le-v77nmxQX3W4/view?usp=sharing)

**Sign in**

[![Sign in popup](https://drive.google.com/uc?export=view&id=1Gq2ZkIWNtutItv3yJu-HCfzWkaC1PvFP)](https://drive.google.com/file/d/1Gq2ZkIWNtutItv3yJu-HCfzWkaC1PvFP/view?usp=sharing)

**Today's unwatched tracked shows**

[![Popup schedule](https://drive.google.com/uc?export=view&id=1P_iXyveP_6RH_S9U4gxO-oSpBq1YijTY)](https://drive.google.com/file/d/1P_iXyveP_6RH_S9U4gxO-oSpBq1YijTY/view?usp=sharing)

**Manage shows — search, sidebar schedule, add/remove tracking**

[![Manage page](https://drive.google.com/uc?export=view&id=1V8TDY-GilIrkM3Ycovd8yiG2t4ZbH67a)](https://drive.google.com/file/d/1V8TDY-GilIrkM3Ycovd8yiG2t4ZbH67a/view?usp=sharing)

## What it does

Every day at midnight UTC, a scheduled Azure Container Apps Job pulls the day's anime release schedule from a third-party API and syncs it into a shared Azure SQL database. Users sign in through Microsoft Entra, search the full catalog of ~19,000 titles, and follow the series they want to track.

The popup surfaces only the shows airing today that the user hasn't watched yet. Clicking a show resolves the correct streaming link via a backend proxy — the extension queries AnimeShedule's API for the show's AniList ID, passes it to a modular streaming provider, and opens the right episode directly. Watch detection runs as a content script: if the user stays on a streaming page for five or more minutes, the episode is logged as watched and removed from the popup automatically. Under five minutes is treated as a misclick.

## Architecture

**Firefox Extension** (JavaScript, WebExtensions API)
Sign-in via Microsoft identity, popup showing today's unwatched tracked shows with one-click streaming, a management page for browsing and tracking series, browser notifications when tracked shows air, and a content script for passive watch-time detection.

**User Service** (ASP.NET Core / C#)
REST API for authentication, tracked series, watch history, schedule reads, and streaming link resolution via a backend proxy to AnimeShedule. Backed by Entity Framework Core against Azure SQL.

**Schedule Sync Job** (.NET console app, Azure Container Apps Job)
Daily cron job that calls the AnimeShedule API and writes the day's releases into the shared database. Completely decoupled from the user-facing API — if the sync fails, the rest of the system keeps running off the last successful data.

**Azure SQL Database**
Shared persistent store for users, tracked series, watch history, the daily schedule, and the full anime catalog.

## Streaming Provider System

The extension ships with a modular provider architecture in `Extension/providers/`. Each provider is a single file that implements whether it can handle a given show and how to construct the watch URL from an AniList ID, slug, and episode number.

```javascript
// miruro.source.js
export default {
  name: "Miruro",
  canHandle: (anilistId) => !!anilistId,
  buildUrl: (anilistId, slug, episode) =>
    `https://www.miruro.to/watch/${anilistId}/${slug}?ep=${episode}`
}
```

Adding support for a new streaming site is one file. The AniList ID is resolved at click time via a backend call to AnimeShedule's API, so no additional database columns or reseeding is required.

## Infrastructure

- **Azure Container Apps** — hosts the user service with staging and production revisions, traffic-shifted automatically on every deploy
- **Azure Container Apps Jobs** — runs the schedule sync as a scheduled, stateless container job
- **Azure Container Registry** — stores Docker images built and pushed by CI/CD
- **Azure SQL Database** — relational store for all application data
- **Azure Storage Account** — backing store for the Container Apps Jobs runtime
- **Microsoft Entra ID** — handles user authentication; the application never stores passwords
- **Bicep** — infrastructure defined as code in `infrastructure/bicep/`, formalizing the Azure environment for reproducibility

## CI/CD

GitHub Actions runs on every push to `main`:

1. **CI** — restores, builds, and runs the xUnit test suite against an in-memory EF Core database
2. **CD** — builds a Docker image tagged with the commit SHA, pushes it to Azure Container Registry, deploys it to a new staging revision on Azure Container Apps, then shifts 100% of traffic to that revision
3. Old revisions are deactivated automatically after each deploy

## Database Schema

- `users` — accounts linked to Microsoft Entra object IDs; no passwords stored
- `anime_catalog` — full catalog of ~19,000 titles, seeded once and updated daily as new shows air
- `daily_schedule` — episodes airing on a given date, referenced by slug
- `tracked_series` — per-user list of followed shows
- `watch_history` — per-episode records with `minutes_watched`; entries under 5 minutes are treated as misclicks and do not remove shows from the popup

## Local Development

```bash
cd services/user-service
dotnet restore
dotnet run
```

Run tests:

```bash
cd services/user-service.Tests
dotnet test
```

Load the extension in Firefox via `about:debugging` → This Firefox → Load Temporary Add-on → select `Extension/manifest.json`.

## Adding a New Streaming Provider

Create a file in `Extension/providers/` implementing the provider interface, then register it in `index.js`. No backend changes, no database changes.

```javascript
export default {
  name: "YourSite",
  canHandle: (anilistId) => !!anilistId,
  buildUrl: (anilistId, slug, episode) =>
    `https://yoursite.com/watch/${anilistId}/${slug}?ep=${episode}`
}
```

## Planned

- Additional streaming providers
- Episode-level deep links where site URLs support it
- Expanded test coverage across all services
- Platform picker UI when multiple providers match a show
