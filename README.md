# Anime Tracker - Firefox Extension + Azure Microservices Platform

A Firefox browser extension that tracks daily anime releases and notifies users when episodes from their followed series air. Built as a distributed system on Azure, with a Firefox extension frontend, multiple backend services, and a fully automated CI/CD pipeline.

## What it does

Every day, a scheduled Azure Container Apps Job pulls the day's anime release schedule from a third-party API and syncs it into a shared Azure SQL database. Users sign in through Microsoft authentication, search and follow series they're interested in, and the extension surfaces only the shows airing today that they haven't watched yet. Watch detection works by measuring time spent on a streaming page - under five minutes is treated as a misclick rather than an actual watch.

## Architecture

The system is split into independently deployable services rather than a single monolith, communicating over REST and sharing a central Azure SQL database.

**Firefox Extension** (TypeScript/JavaScript)
Handles sign-in via Microsoft identity, displays the day's unwatched tracked shows in the popup, and provides a management page for searching the full anime catalog and adding or removing tracked series.

**User Service** (ASP.NET Core / C#)
REST API exposing endpoints for authentication, tracked series, watch history, and schedule reads. Backed by Entity Framework Core against Azure SQL.

**Schedule Sync Job** (.NET console app, Azure Container Apps Job)
Runs on a daily cron schedule, calling an external anime schedule API and writing the day's releases into the shared database. Decoupled entirely from the user-facing API - if this job fails, the rest of the system keeps working off the last successful sync.

**Azure SQL Database**
Shared persistent store for users, tracked series, watch history, the daily schedule, and the full anime catalog (~19,000 titles) that grows every release.

## Infrastructure

- **Azure Container Apps** - hosts the user service with staging and production revisions, traffic-shifted automatically on deploy (blue/green style)
- **Azure Container Apps Jobs** - runs the schedule sync as a scheduled, stateless job rather than a long-running service
- **Azure Container Registry** - stores Docker images built by CI/CD
- **Azure SQL Database** - relational store for all application data
- **Azure Storage Account** - backing store required by the Container Apps Jobs runtime
- **Microsoft Entra ID** - handles user authentication; the application never stores passwords
- **Bicep** - infrastructure defined as code in `infrastructure/bicep/`. Initial resources were provisioned through the Azure Portal during early development and later formalized as Bicep for reproducibility

## CI/CD

GitHub Actions runs on every push to `main`:

1. **CI** - restores, builds, and runs the xUnit test suite against an in-memory EF Core database
2. **CD** - builds a Docker image tagged with the commit SHA, pushes it to Azure Container Registry, deploys it to a new staging revision, then shifts 100% of traffic to that revision once it's live
3. Old revisions are deactivated automatically after each deploy to keep the environment clean

## Screenshots

**Database schema**
[![Database schema](https://drive.google.com/uc?export=view&id=1UITmOekanWsNpEodd3le-v77nmxQX3W4)](https://drive.google.com/file/d/1UITmOekanWsNpEodd3le-v77nmxQX3W4/view?usp=sharing)

**Sign in**
[![Sign in popup](https://drive.google.com/uc?export=view&id=1Gq2ZkIWNtutItv3yJu-HCfzWkaC1PvFP)](https://drive.google.com/file/d/1Gq2ZkIWNtutItv3yJu-HCfzWkaC1PvFP/view?usp=sharing)

**Today's unwatched tracked shows**
[![Popup schedule](https://drive.google.com/uc?export=view&id=1P_iXyveP_6RH_S9U4gxO-oSpBq1YijTY)](https://drive.google.com/file/d/1P_iXyveP_6RH_S9U4gxO-oSpBq1YijTY/view?usp=sharing)

**Manage shows - search, sidebar schedule, add/remove tracking**
[![Manage page](https://drive.google.com/uc?export=view&id=1V8TDY-GilIrkM3Ycovd8yiG2t4ZbH67a)](https://drive.google.com/file/d/1V8TDY-GilIrkM3Ycovd8yiG2t4ZbH67a/view?usp=sharing)

## Database schema

- `users` - accounts linked to Microsoft Entra identities
- `anime_catalog` - full catalog of tracked-able series, synced and updated by the daily job
- `daily_schedule` - episodes airing on a given date, references the catalog by slug
- `tracked_series` - which series a user follows
- `watch_history` - per-episode watch records, including minutes watched for the five-minute watch-detection rule

## Local development

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

Load the extension unpacked in Firefox via `about:debugging` → This Firefox → Load Temporary Add-on → select `Extension/manifest.json`.

## Adding a new schedule data source

The system is designed so the upstream data provider can be swapped without touching the rest of the application. To add a new source, implement the provider interface in `Extension/providers/` and update the schedule sync job to call it - the database schema and API layer require no changes.

## Status

Core functionality - authentication, daily schedule sync, tracked series, search, and CI/CD with staging/production deploys - is complete and running in Azure. Browser notifications and full automated test coverage across all services are in progress.
