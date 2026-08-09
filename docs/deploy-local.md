# Local Development

Run the app locally with **.NET Aspire** during development. Aspire orchestrates the API and Web
services, provides a dashboard (logs, traces, metrics, health), and handles service discovery
automatically — no manual URL configuration needed.

## Prerequisites

| Tool | Install |
| --- | --- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Required for building and running |
| [.NET Aspire CLI](https://aspire.dev) | `irm https://aspire.dev/install.ps1 \| iex` (Windows PowerShell), then `dotnet new install Aspire.ProjectTemplates` |
| [Docker Desktop](https://www.docker.com/) | Required for the Aspire dashboard |

## Configure authentication secrets

Store the Google OAuth credentials and JWT signing key in the AppHost's user secrets:

```powershell
cd src/MealPlanner.AppHost
dotnet user-secrets set "Parameters:jwt-key" "<a-random-256-bit-key>"
dotnet user-secrets set "Parameters:google-client-id" "<your-google-client-id>"
dotnet user-secrets set "Parameters:google-client-secret" "<your-google-client-secret>"
```

> **Tip:** Generate a random key with:
> `[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Max 256 }) -as [byte[]])`

See the [Google OAuth setup](#setting-up-google-oauth-credentials) section below if you don't
already have credentials.

## Run with Aspire

```powershell
aspire run
```

This starts the AppHost, which launches the **API** and **Web** services and opens the Aspire
**dashboard** (logs, traces, metrics, health). The Web UI reaches the API through Aspire service
discovery — you don't configure URLs by hand.

## Run just the API

To run only the API (e.g. to inspect endpoints with Swagger or `curl`):

```powershell
dotnet run --project src/MealPlanner.Api
```

On startup the API ensures the database directory exists, backs up the SQLite file when there are
pending migrations, applies migrations, and enables WAL mode.

## Build & test

```powershell
dotnet build MealPlanner.slnx
dotnet test MealPlanner.slnx
```

## Setting up Google OAuth credentials

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project (or reuse an existing one).
3. Navigate to **APIs & Services → Credentials → Create Credentials → OAuth client ID**.
4. Set application type to **Web application**.
5. Under **Authorized redirect URIs**, add:
   - `https://localhost:<port>/signin-google` (for local development)
   - `https://<your-domain>/signin-google` (for Docker deployment behind a reverse proxy)
6. Copy the **Client ID** and **Client Secret**.

## Canadian Nutrient File (optional)

Ingredient nutrition can be populated from the **Canadian Nutrient File (CNF)**.

1. Download the CSV dataset (~2.8 MB):
   <https://www.canada.ca/content/dam/hc-sc/migration/hc-sc/fn-an/alt_formats/zip/nutrition/fiche-nutri-data/cnf-fcen-csv.zip>
2. Unzip the CSVs into `data/cnf/` at the repository root (at minimum `FOOD NAME.csv` and
   `NUTRIENT AMOUNT.csv`).
3. The API reads the dataset lazily and caches it in memory. When the files are absent the CNF
   search is simply hidden in the UI.

The development profile points `MealPlanner:CnfDirectory` at the repo-root `data/cnf/` folder.
