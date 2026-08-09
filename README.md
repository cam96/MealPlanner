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

## Run locally (Aspire)

```powershell
aspire run
```

This starts the AppHost, which launches the **API** and **Web** services and opens the Aspire
**dashboard** (logs, traces, metrics, health). The Web UI reaches the API through Aspire service
discovery — you don't configure URLs by hand.

To run just the API (for example to inspect endpoints):

```powershell
dotnet run --project src/MealPlanner.Api
```

On startup the API ensures the database directory exists, backs up the SQLite file when there are
pending migrations, applies migrations, and enables WAL mode.

## Deploy to a home server (Docker Compose)

The app is packaged as two small containers (**API** + **Web**) behind a **Caddy** reverse proxy,
orchestrated by the checked-in [docker-compose.yml](docker-compose.yml), with a multi-stage
Dockerfile per service. Caddy terminates TLS using a Cloudflare Origin Certificate so the app is
served over HTTPS on port 443. Because the images are built **from source inside Docker**, the server
needs only Docker — no .NET SDK, no Aspire tooling, and no manual publish step. The whole stack comes
up with a single command.

### 1. Install on the server

The server can run any OS that supports Docker (Linux is recommended for a headless home server;
Windows and macOS work too). Install:

- **Docker Engine + Docker Compose v2** — the only required dependency.
  - **Linux (Debian/Ubuntu):**

    ```bash
    curl -fsSL https://get.docker.com | sh
    sudo usermod -aG docker $USER   # then log out/in so `docker` runs without sudo
    ```

    This installs Docker Engine and the `docker compose` plugin. Verify with `docker --version`
    and `docker compose version`.
  - **Windows / macOS:** install [Docker Desktop](https://www.docker.com/products/docker-desktop/),
    which includes Compose.

No other runtime is needed on the server — .NET, EF Core, and every NuGet package are compiled and
bundled into the images during the build.

### 2. Copy the repository to the server

The Docker build needs the **whole repository** (the build context is the repo root so the shared
`Directory.Build.props` / `Directory.Packages.props` and all projects are available during restore).
Copy the entire working tree to the server — the simplest options:

- **Git (recommended):** clone the repo directly on the server so updates are just `git pull`.

  ```bash
  git clone <your-repo-url> mealplanner
  cd mealplanner
  ```

- **SCP / rsync from your dev machine** (skip build output to keep the copy small):

  ```powershell
  # from the repository root on your PC (PowerShell)
  scp -r . <user>@<server-ip>:/home/<user>/mealplanner
  ```

  ```bash
  # or with rsync (Linux/macOS/WSL), excluding local build artifacts
  rsync -av --exclude 'bin/' --exclude 'obj/' --exclude 'data/mealplanner.db' \
    ./ <user>@<server-ip>:/home/<user>/mealplanner/
  ```

You do **not** need to copy `bin/`, `obj/`, or any locally built database — those are rebuilt or
created on the server. Named Docker volumes (not files in the repo) hold the live data.

### 3. Set up the Cloudflare Origin Certificate

The domain `mealplanner.cameronmckay.ca` is managed by Cloudflare with proxy mode (orange cloud)
enabled. Cloudflare terminates TLS for visitors; the Origin Certificate secures the
Cloudflare → origin connection.

1. In the Cloudflare dashboard, go to **SSL/TLS → Origin Server → Create Certificate**.
2. Choose **RSA (2048)**, enter `mealplanner.cameronmckay.ca` as the hostname, and set the
   validity (up to 15 years).
3. Save the **certificate** as `certs/origin.crt` and the **private key** as `certs/origin.key`
   in the repository root on the server.

```bash
mkdir -p certs
# paste/copy the cert and key files into this directory
ls certs/
# origin.crt  origin.key
```

> The `certs/` directory is gitignored — private keys must never be committed.

### 4. Run the app on the server

From the copied repository root on the server:

```bash
docker compose up -d --build
```

This builds both images and starts the containers detached. The API mounts named volumes for the
database (`/data`) and rotating pre-migration backups (`/backups`) so your household data survives
container rebuilds and updates; both services use `restart: unless-stopped`, so they come back
automatically after a reboot or crash. On first start the API creates the database directory,
applies EF Core migrations, and enables WAL mode.

Browse the app at **`https://mealplanner.cameronmckay.ca`** (Cloudflare proxied). Only **Caddy**
publishes a host port (443); the **Web** and **API** containers are internal-only on the Docker
network and cannot be reached directly from outside.

To load representative **demo data** on a fresh install, set `MealPlanner__SeedDemoData: "true"` on
the `api` service in [docker-compose.yml](docker-compose.yml) before the first `up` (seeding runs
only when the database is empty). Leave it `"false"` to start clean.

### 5. Manage, update, and view logs

```bash
docker compose ps                 # container status and health
docker compose logs -f            # follow logs (add a service name to scope: api / web)
docker compose down               # stop and remove containers (named volumes/data are kept)
```

#### Update from source (building locally)

To deploy a new version after pulling code changes, rebuild and recreate in place — the data
volumes are untouched:

```bash
git pull                          # or re-copy the repo
VERSION=1.2.3 docker compose up -d --build   # versioned build
```

#### Update from pre-built images (recommended)

If your deployment uses the pre-built GHCR images (see [Deploy from a release](#deploy-from-a-release)),
pull the latest and replace the running containers:

```bash
docker compose pull               # pull the latest images from GHCR
docker compose up -d              # recreate containers with the new images
```

The `VERSION` variable is baked into the published assemblies and displayed in the web UI (bottom of
the sidebar). Omit it for a local dev build (`0.0.0-dev`).

If the CNF CSV files are present in `data/cnf/` at build time, they are bundled into the API image
so the deployment is fully self-contained. When absent, the image still builds and CNF search is
simply hidden in the UI.

> `aspire publish` can also generate an equivalent Compose project; the checked-in
> [docker-compose.yml](docker-compose.yml) is the maintained home-deploy artifact.

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
   update the `:latest` tag. When **automatic deployment** is enabled, a follow-up job SSHes into
   the home server, pulls the latest images, and restarts the stack.

The Dockerfiles accept a `VERSION` build arg and pass it to `dotnet publish /p:Version=${VERSION}`.
The web UI reads the assembly `InformationalVersion` at runtime and displays it at the bottom of the
navigation drawer.

### Deploy from a release

Images are published to **GitHub Container Registry** (GHCR) on every release. Pull the latest
images and start the stack:

```bash
docker compose pull               # pulls ghcr.io/cam96/mealplanner-api:latest and mealplanner-web:latest
docker compose up -d              # starts containers from the pulled images
```

To pin a specific version:

```bash
VERSION=1.0.0 docker compose pull
docker compose up -d
```

#### Update a running deployment to the latest release

When a new release is published, pull the updated images and recreate the containers in place —
existing data volumes are preserved:

```bash
docker compose pull               # fetches the newest :latest images from GHCR
docker compose up -d              # recreates only containers whose image changed
```

This is zero-downtime for the data: named volumes (`mealplanner-data`, `mealplanner-backups`) are
**not** removed. The API applies any pending EF Core migrations on startup (after backing up the
database). If you want to force-recreate both containers even when the image hasn't changed:

```bash
docker compose up -d --force-recreate
```

#### Offline deployment from release tarballs

Alternatively, download the image tarballs from a
[GitHub Release](https://github.com/cam96/MealPlanner/releases) for offline deployment:

```bash
gunzip MealPlanner-Api-1.0.0.tar.gz MealPlanner-Web-1.0.0.tar.gz
docker load -i MealPlanner-Api-1.0.0.tar
docker load -i MealPlanner-Web-1.0.0.tar
docker compose up -d
```

Because the images are pre-built with the version already embedded, no source code or .NET SDK is
needed on the server — just Docker.

#### Automatic deployment from releases

The release workflow includes an optional **deploy** job that SSHes into the home server and
restarts the stack after a successful release. To enable it:

1. **Create a `production` environment** in the repo (Settings → Environments → New environment).
2. **Set the repository variable** `DEPLOY_ENABLED` to `true` (Settings → Variables → New variable).
3. **Add these secrets** to the `production` environment:

   | Secret | Value |
   | --- | --- |
   | `DEPLOY_HOST` | Public IP or hostname of the home server |
   | `DEPLOY_USER` | SSH username on the server |
   | `DEPLOY_SSH_KEY` | SSH private key (the corresponding public key must be in `~/.ssh/authorized_keys` on the server) |
   | `DEPLOY_PORT` | SSH port (optional, defaults to 22) |
   | `DEPLOY_PATH` | Absolute path to the repo clone on the server (e.g., `/home/cam/mealplanner`) |

When enabled, the deploy job runs after the release job succeeds: it does a `git pull --ff-only` to
pick up any compose/Caddyfile changes, pulls the latest images, and runs `docker compose up -d`.

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
- [Copilot project instructions](.github/copilot-instructions.md)
