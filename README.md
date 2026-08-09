# MealPlanner

A local-network **meal-planning web app** for a two-person household in Winnipeg, Manitoba. Plan a
month of meals (broken down by week) that hits each person's daily
**calorie / protein / fibre / carbs / fat** goals, keeps grocery spending on **budget**, and respects
busy-day schedules and per-recipe prep time. Runs in **Docker** on your home network.

> Status: **Feature-complete (Phases 0–8)**. The app plans a month of meals, tracks per-person
> nutrition against goals, generates a pantry-aware shopping list within a monthly budget, and
> surfaces it all on a dashboard — packaged to run on the home network with `docker compose up`.
> Nutrition, cost, planning, shopping and dashboard calculators live in a pure, unit-tested domain
> layer; only the API touches the SQLite database (backed up before every migration, on a Docker
> volume with WAL). Ingredient nutrition can be populated from the **Canadian Nutrient File (CNF)**
> when its dataset is present. See the [project plan](specs/meal-planner-plan.md) and
> [architecture](specs/architecture.md).

## Tech stack

- **.NET 10** (LTS) · **ASP.NET Core Web API** + **Blazor Web App** (Interactive Server)
- **.NET Aspire** orchestration (dashboard, service discovery, health, resilience, OpenTelemetry)
- **EF Core 10 + SQLite** (file database on a Docker volume, WAL mode; only the API touches it)
- **MudBlazor** UI with a custom earth-tones theme
- **NUnit** tests

## Solution layout

| Project | Role |
| --- | --- |
| `src/MealPlanner.AppHost` | Aspire orchestrator (entry point for `aspire run`) |
| `src/MealPlanner.ServiceDefaults` | OpenTelemetry, health checks, resilience, service discovery |
| `src/MealPlanner.Domain` | Entities + calculators (pure C#, unit-tested) |
| `src/MealPlanner.Data` | EF Core `DbContext`, configs, migrations, CNF importer |
| `src/MealPlanner.Contracts` | DTOs shared by API + Web |
| `src/MealPlanner.Api` | Web API, validation, composition root, startup migration |
| `src/MealPlanner.Web` | Blazor + MudBlazor UI (calls the API via service discovery) |
| `tests/MealPlanner.Tests` | NUnit tests |

## Features

### Core data (Phase 1)

Manage the reference data meal planning is built on:

- **People & goals** (`/people`) — household members with daily calorie, protein, fibre, carbohydrate, and fat targets.
- **Ingredients** (`/ingredients`) — foods with per-100 g/ml nutrition; supports gram, millilitre,
  and each (count) base units, an optional per-item serving weight, and an **estimated** nutrition
  flag that is visibly marked in the UI.
- **Stores** (managed under `/settings`) — grocery stores; **Costco**, **Superstore**, and
  **Safeway** are seeded by default. A store can't be deleted while it has recorded prices.
- **Prices** (`/prices`) — per-store price observations for an ingredient over time, with package
  quantity/unit, a preferred-store marker, and an **estimated** price flag.

All of the above are served by the API and consumed by the Web UI through the typed
`MealPlannerApiClient`; the Web project never touches the database directly. Enums are exchanged as
strings over HTTP.

### Recipes (Phase 2)

- **Recipes** (`/recipes`) — build recipes from ingredient lines (quantity + unit per line), with
  meal type, prep/cook times, servings, and instructions. The API computes **per-serving nutrition**
  (calories, protein, fibre, carbs, fat) and **per-serving + total cost** from recorded ingredient prices:
  - Quantities are converted to each ingredient's base unit (grams, millilitres, or counts via the
    per-item serving weight) before scaling per-100 nutrition values.
  - Cost uses the **preferred store's most recent price**, falling back to the latest price at any
    store, prorated to the quantity used.
  - Results carry **estimate flags** (visibly badged in the UI) whenever a value is estimated or a
    line can't be priced/converted.
- The recipe **editor** (`/recipes/new`, `/recipes/{id}`) has an ingredient autocomplete per line
  and a per-serving nutrition/cost panel that recalculates on save.

### Pantry & freezer (Phase 4)

- **Pantry** (`/pantry`) — track quantities of ingredients on hand by **storage location** (pantry,
  fridge, or freezer). The inventory grid groups by location, and deleting an ingredient is blocked
  while it is still stocked. This inventory feeds later meal-planning and shopping-list phases so
  the app doesn't suggest buying what you already have.

### Meal planner (Phase 5)

- **Planner** (`/planner`) — a monthly calendar (weekly rows) for assigning recipes to meal slots.
  A month's day plans are generated on first visit.
  - Assign a **recipe** or a **meal combo** (see below) to a **slot** (breakfast, lunch, dinner,
    snack) for the **first person**, the **second person**, or **shared**. Shared meals split their
    servings evenly across the household.
  - Mark a day as **normal**, **eating out**, or **event** (with an optional note). Only normal days
    count toward nutrition goals and prep load.
  - Each day shows **per-person nutrition versus daily goals** (calories, with estimate asterisks)
    and the total **prep + cook minutes** planned. Deleting a recipe is blocked while it is planned.

### Meal categories & combos

- **Meal categories** (`/categories`) — every ingredient can be filed under exactly one food
  category: **Protein**, **Carbohydrate**, or **Vegetable** (or left uncategorized). A category can
  be set when **adding** or editing an ingredient, and the category is shown as a column on the
  ingredients list.
- The page is a **three-column board** (one column per category) listing that category's ingredients
  alongside their **pantry and freezer stock on hand**, so you can see what's available at a glance.
  Each column's **Add** button either files an existing ingredient into that category (the picker
  shows any food already tagged for another column, and choosing it moves it here) or lets you
  **create a brand-new ingredient straight into that category**.
- **Meal combos** — informal, reusable dinner ideas that pair up to one protein, one carbohydrate
  and one vegetable. Unlike a recipe, a combo carries **no quantities or instructions**; it is a
  rough pairing of foods. Combos can be **saved for reuse** and **added to the monthly planner** just
  like a recipe. On the planner, combo meals are marked **"no nutrition"** since they don't
  contribute a nutrition estimate. Deleting a combo is blocked while it is still used by a planned
  meal.

### Shopping list & budget (Phase 6)

- **Shopping list** (`/shopping-list`) — generated from a month's planned meals: ingredient
  quantities are aggregated across normal days, **reduced by pantry stock**, and priced at each
  ingredient's **preferred store** (falling back to the most recent price). The list groups by
  store and computes whole **packages to buy** plus a total, with badges for:
  - **Deal** — the latest price is at least 10% below the ingredient's historical average.
  - **Bulk** — a single package substantially exceeds what you need.
  - **Shared** — the ingredient is used across more than one recipe.
  - Estimated or unpriced costs are marked with an asterisk.
- **Manual items** — users can add free-text items to the shopping list (e.g. cleaning supplies,
  snacks) with optional quantity and unit, independent of any meal plan.
- **Cart check-off** — every item (generated or manual) has a checkbox; checking it moves the
  item to an **"In the Cart"** section with struck-through styling. A **Clear Cart** button
  removes all carted items at once (manual items are deleted; generated items are un-carted).
- **Budget** (managed under `/settings`) — set a **monthly grocery budget**; the shopping list shows
  the estimated total against it with an over/under indicator.

### Dashboard (Phase 7)

- **Dashboard** (the home page, `/`) — an at-a-glance monthly summary combining the planner,
  shopping and budget data:
  - **Nutrition** — each person's **average daily** calories, protein, fibre, carbs and fat across the month's
    normal days, shown as progress bars against their goals (estimates marked with an asterisk).
  - **Budget** — projected grocery spend versus the monthly budget, with an over/under gauge.
  - **Cooking load** — total and average prep + cook minutes, plus the busiest day.
  - **Alerts** — warnings when a person averages well under/over a calorie goal or spend exceeds the
    budget, and informational notes for low protein or normal days with no meals planned.

### Deployment & demo data (Phase 8)

- **Docker Compose stack** — a checked-in [docker-compose.yml](docker-compose.yml) with a
  multi-stage Dockerfile per service brings the whole app up with one command, using named volumes
  for the database and backups, `unless-stopped` restart, and an API health check.
- **Demo data seeder** — an optional, idempotent seeder loads representative people, ingredients,
  prices, recipes, pantry stock and a monthly budget on a fresh install (gated by
  `MealPlanner__SeedDemoData`).

#### API endpoints

| Resource | Endpoints |
| --- | --- |
| People | `GET/POST /api/people`, `GET/PUT/DELETE /api/people/{id}` |
| Stores | `GET/POST /api/stores`, `GET/PUT/DELETE /api/stores/{id}` |
| Ingredients | `GET/POST /api/ingredients`, `GET/PUT/DELETE /api/ingredients/{id}`, `PUT /api/ingredients/{id}/category` |
| Prices | `GET/POST /api/ingredients/{ingredientId}/prices`, `PUT/DELETE /api/ingredients/{ingredientId}/prices/{priceId}` |
| Recipes | `GET/POST /api/recipes`, `GET/PUT/DELETE /api/recipes/{id}` |
| Pantry | `GET/POST /api/pantry`, `GET/PUT/DELETE /api/pantry/{id}` |
| Planner | `GET /api/plans/{year}/{month}`, `PUT /api/plans/days/{dayId}`, `POST /api/plans/days/{dayId}/meals`, `PUT/DELETE /api/plans/meals/{mealId}` |
| Combos | `GET /api/combos/board`, `GET/POST /api/combos`, `PUT/DELETE /api/combos/{id}` |
| Shopping | `GET /api/plans/{year}/{month}/shopping-list`, `POST/DELETE .../manual-items`, `PUT .../manual-items/{id}/cart`, `PUT .../items/{ingredientId}/cart`, `DELETE .../cart` |
| Settings | `GET/PUT /api/settings` |
| Dashboard | `GET /api/plans/{year}/{month}/dashboard` |
| Canadian Nutrient File | `GET /api/cnf/status`, `GET /api/cnf/foods?query=`, `GET /api/cnf/foods/{foodCode}` |

All API endpoints except `/ping` and health checks require a valid JWT Bearer token. Tokens are
issued by the Web front-end after Google OAuth login.

## Authentication & authorization

The application requires authentication to access any page or API endpoint.

- **Web front-end** — Google OAuth login with cookie-based session. Unauthenticated users are
  redirected to the login page.
- **API** — JWT Bearer authentication. The Web server generates a short-lived JWT (signed with a shared
  HMAC key) after the user authenticates via Google and attaches it to every outbound API call.

### Setting up Google OAuth credentials

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project (or reuse an existing one).
3. Navigate to **APIs & Services → Credentials → Create Credentials → OAuth client ID**.
4. Set application type to **Web application**.
5. Under **Authorized redirect URIs**, add:
   - `https://localhost:<port>/signin-google` (for local development)
   - `https://<your-domain>/signin-google` (for Docker deployment behind a reverse proxy)
6. Copy the **Client ID** and **Client Secret**.

Secret configuration is covered in each deployment guide:
- [Local development](docs/deploy-local.md#configure-authentication-secrets) — Aspire user secrets
- [Home server / cloud](docs/deploy-home-server.md#1-configure-authentication-secrets) — `.env` file

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [.NET Aspire CLI](https://aspire.dev): `irm https://aspire.dev/install.ps1 | iex` (Windows
  PowerShell), then `dotnet new install Aspire.ProjectTemplates`
- [Docker Desktop](https://www.docker.com/) (for the Aspire dashboard and for deployment)

## Build & test

```powershell
dotnet build MealPlanner.slnx
dotnet test MealPlanner.slnx
```

## Deployment

| Scenario | Guide | Summary |
| --- | --- | --- |
| **Local development** | [docs/deploy-local.md](docs/deploy-local.md) | Run with `aspire run`; Aspire handles service discovery and dashboard |
| **Home server** | [docs/deploy-home-server.md](docs/deploy-home-server.md) | Docker Compose + Caddy reverse proxy with Cloudflare TLS |
| **Cloud / VPS** | [docs/deploy-cloud.md](docs/deploy-cloud.md) | Same Docker stack on a remote VM with Let's Encrypt or Cloudflare |

### Quick start (local)

```powershell
aspire run
```

### Quick start (home server)

```bash
docker compose up -d --build      # build from source
# — or —
docker compose pull && docker compose up -d   # pull pre-built images from GHCR
```

See the deployment guides for full setup instructions (secrets, TLS certificates, updates).

## Versioning & releases

The application version flows from one source:

1. **Default** — `Directory.Build.props` sets `<Version>0.0.0-dev</Version>` (shown in the web UI
   sidebar for local/dev builds).
2. **Deployment** — pass `VERSION=x.y.z` to `docker compose build` (or set it in the environment)
   to bake a real version into both images.
3. **GitHub Release** — the [release workflow](.github/workflows/release.yml) (triggered manually via
   `workflow_dispatch`) validates the build, runs tests, pushes both Docker images to **GitHub
   Container Registry** (`ghcr.io/cam96/mealplanner-api` and `mealplanner-web`) tagged with the
   version (and `:latest` for stable releases), and attaches compressed image tarballs
   (`MealPlanner-Api-VERSION.tar.gz`, `MealPlanner-Web-VERSION.tar.gz`) to the GitHub Release.
   Versions containing a hyphen (e.g., `1.0.0-alpha.1`) are marked as pre-releases and do not
   update the `:latest` tag.

The Dockerfiles accept a `VERSION` build arg and pass it to `dotnet publish /p:Version=${VERSION}`.
The web UI reads the assembly `InformationalVersion` at runtime and displays it at the bottom of the
navigation drawer.

### GHCR privacy (maintainers)

Container images pushed by the release workflow inherit the repository owner's default package
visibility. To ensure images stay **private**:

1. Go to **GitHub → Settings → Packages → Package creation**.
2. Set the default visibility to **Private**.

This must be confirmed before the first release push. Visibility can also be changed per-package
after publishing.

## Data safety

- The SQLite database lives on a **named Docker volume** (`mealplanner-data` → `/data`), never in
  the container layer.
- **WAL** journal mode is enabled for safer concurrent read/write.
- A **pre-migration backup** is written to the `mealplanner-backups` volume (`/backups`) before any
  migration is applied.
- Schema changes use EF Core **migrations** only, with an **expand/contract** approach for
  destructive changes. To add a migration:

  ```powershell
  dotnet ef migrations add <Name> --project src/MealPlanner.Data
  ```

- **Back up / restore** the live database (stop the stack first for a consistent copy):

  ```powershell
  docker compose stop api
  docker run --rm -v mealplanner_mealplanner-data:/data -v ${PWD}:/out `
    busybox cp /data/mealplanner.db /out/mealplanner.backup.db   # export
  docker run --rm -v mealplanner_mealplanner-data:/data -v ${PWD}:/in `
    busybox cp /in/mealplanner.backup.db /data/mealplanner.db    # restore
  docker compose start api
  ```

## Nutrition data (Canadian Nutrient File)

Ingredient nutrition can be populated from the **Canadian Nutrient File (CNF)**, read locally by the
API (never committed to source control).

1. Download the CSV dataset (~2.8 MB):
   <https://www.canada.ca/content/dam/hc-sc/migration/hc-sc/fn-an/alt_formats/zip/nutrition/fiche-nutri-data/cnf-fcen-csv.zip>
   (from the [CNF 2015 download page](https://www.canada.ca/en/health-canada/services/food-nutrition/healthy-eating/nutrient-data/canadian-nutrient-file-2015-download-files.html)).
2. Unzip the CSVs into `data/cnf/` at the repository root (at minimum `FOOD NAME.csv` and
   `NUTRIENT AMOUNT.csv`).
3. In the **Ingredients** editor, use *Populate from Canadian Nutrient File* to search foods and
   fill in calories, protein, fibre, carbs and fat per 100 g (nutrient codes 208 = kcal, 203 = protein,
   291 = fibre, 205 = carbohydrate, 204 = fat); the ingredient is linked to the CNF food code and no
   longer marked as an estimate.

The API reads the dataset lazily and caches it in memory; when the files are absent the CNF search
is simply hidden. The directory is configured by `MealPlanner:CnfDirectory` (defaults to `data/cnf`;
the development profile points at the repo-root copy). The Docker build bundles `data/cnf/` into
the API image automatically — just drop the CSVs into `data/cnf/` before building.

Attribution shown in the UI: **"Canadian Nutrient File, Health Canada, 2015"**.

## Documentation

- [Project plan](specs/meal-planner-plan.md)
- [Architecture & diagrams](specs/architecture.md)
- [Deploy locally](docs/deploy-local.md)
- [Deploy to a home server](docs/deploy-home-server.md)
- [Deploy to cloud / VPS](docs/deploy-cloud.md)
- [Copilot project instructions](.github/copilot-instructions.md)
