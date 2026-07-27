# MealPlanner — Project Plan

## Goal

A local-network web app for a Winnipeg, Manitoba couple to plan monthly meals (broken down
weekly) that:

- hit each person's daily calorie / protein / fibre / carbs / fat goals,
- control grocery spending against a monthly budget (target **$800–900/mo**, down from
  **$1200–1500/mo**),
- respect schedules and per-recipe prep time.

It runs in Docker on the home network. No authentication — users pick "Me" or "Partner".

## Confirmed decisions

- **Nutrition:** manual entry with suggested lookup from the Canadian Nutrient File (CNF),
  imported locally.
- **Cost:** cost-per-meal matters. Users enter price per ingredient per store at purchase time.
  The app suggests an estimated cost when no concrete data exists and clearly marks estimates.
- **Meal structure:** per-person goals and totals. Dinner is shared. His breakfast + lunch are at
  work (portable); her breakfast + lunch are at home. Planned weekly across a month.
- **Auth:** none (home network only).
- **Run:** Docker containers.
- **Schedule:** per-recipe prep time + busy-day awareness. Days can be flagged "eating out" or
  "event" so no meal is needed in that slot.
- **Shopping list:** one combined list with a preferred store per ingredient. Prices are tracked
  per ingredient per store over time so deals are detectable (current price < historical average).
- **Bulk buy (v1):** flag ingredients shared across meals. A Freezer/Pantry section flags on-hand
  ingredients + quantity so they are excluded from the shopping list.
- **Units:** grams (g) and millilitres (ml) only; counts convert via an optional per-ingredient
  serving weight.

## Tech stack

- **.NET 10** (current LTS), **ASP.NET Core Web API** (decoupled backend) + **Blazor Web App**
  (Interactive Server) UI.
- **.NET Aspire** orchestration: the AppHost runs the Api and Web as separate services with
  service discovery, a dashboard (logs/traces/metrics), health checks, and resilience.
- **EF Core 10 + SQLite** (file DB on a Docker volume, WAL mode). Only the Api touches SQLite.
- **MudBlazor** UI with a custom **earth-tones** theme (warm browns, terracotta, olive/sage green,
  muted ochre/tan, cream backgrounds).
- **NUnit** for tests.
- **Deployment:** `aspire publish` → Docker Compose; home deploy = one `docker compose up`.

See [architecture.md](architecture.md) for the diagrams and layer responsibilities.

## Solution structure

```
MealPlanner.sln (MealPlanner.slnx)
├─ src/
│  ├─ MealPlanner.AppHost          # Aspire orchestrator (entry point for `aspire run`)
│  ├─ MealPlanner.ServiceDefaults  # OTel, health, resilience, service discovery
│  ├─ MealPlanner.Domain           # Entities + calculators (pure C#, unit-tested)
│  ├─ MealPlanner.Data             # EF Core DbContext, configs, migrations, CNF importer
│  ├─ MealPlanner.Contracts        # DTOs shared by Api + Web
│  ├─ MealPlanner.Api              # Web API, validation, composition root (reusable backend)
│  └─ MealPlanner.Web              # Blazor + MudBlazor UI (calls Api via service discovery)
└─ tests/
   └─ MealPlanner.Tests            # NUnit
```

## Data safety (Docker)

- The `.db` file lives on a **named Docker volume** at `/data/mealplanner.db` — it survives
  container rebuild/update and is never in the container writable layer.
- **WAL** journal mode is enabled for safer concurrent read/write.
- Automated **rotating backups** are written to a separate backups volume; restore = copy a file
  back.

## Schema migrations without data loss

1. **Pre-migration backup**: the Api backs up the `.db` file on startup *before* applying
   migrations.
2. Author migrations in dev with `dotnet ef migrations add <Name>`; review the generated SQL
   (SQLite rebuilds tables for many `ALTER`s — confirm no unintended column drops).
3. Apply on startup via `Database.Migrate()` (idempotent; only pending migrations run).
4. Destructive/complex changes use **expand/contract** (add nullable → backfill → switch code →
   drop old column in a later migration). Never rename + drop in one step.
5. Test migrations against a restored copy of the production database before shipping.

## Canadian Nutrient File (CNF)

- Download page: <https://www.canada.ca/en/health-canada/services/food-nutrition/healthy-eating/nutrient-data/canadian-nutrient-file-2015-download-files.html>
- Direct CSV zip (~2.8 MB): <https://www.canada.ca/content/dam/hc-sc/migration/hc-sc/fn-an/alt_formats/zip/nutrition/fiche-nutri-data/cnf-fcen-csv.zip>
- Unzip into `data/cnf/`. The importer reads FOOD NAME, NUTRIENT AMOUNT (codes **208** = kcal,
  **203** = protein, **291** = fibre, **205** = carbohydrate, **204** = fat, per 100 g), and
  CONVERSION FACTOR + MEASURE NAME (portion
  → gram weights).
- Required attribution in the UI: **"Canadian Nutrient File, Health Canada, 2015"**.

## Domain model

- **Person** — Name, DailyCalorieGoal, DailyProteinGoal, DailyFiberGoal, DailyCarbGoal, DailyFatGoal
- **Store** — Name (seed Costco / Superstore / Safeway, editable)
- **Ingredient** — Name, BaseUnit (g/ml/each), CaloriesPer100, ProteinPer100, FiberPer100,
  CarbsPer100, FatPer100, IsNutritionEstimated, CnfFoodCode?, ServingWeightG?
- **IngredientPrice** — StoreId, Price, PackageQuantity, PackageUnit, RecordedDate, IsEstimated,
  IsPreferredStore
- **Recipe** — MealType (Breakfast/Lunch/Dinner), PrepMinutes, CookMinutes, Instructions, Servings
- **RecipeIngredient** — Quantity, Unit
- **PantryItem** — QuantityOnHand, Unit, Location (Freezer/Pantry)
- **MealPlan** — Year, Month
- **DayPlan** — Date, DayType (Normal/EatingOut/Event), Note
- **PlannedMeal** — MealSlot, Assignee (Me/Partner/Shared), RecipeId?, Servings
- **AppSetting** — MonthlyBudget
- **CNF reference** — CnfFood, CnfNutrientAmount (codes 208/203/291/205/204), CnfConversion/Measure

## Calculation services (Domain, unit-tested)

- **NutritionCalculator** — recipe nutrition from ingredients with unit conversion; per-serving and
  per-recipe; propagates `IsEstimated`.
- **CostCalculator** — preferred-store latest price → fallback latest price → estimate; per serving.
- **PlanAggregator** — per-person per-day/week/month totals vs goals; skips EatingOut/Event slots.
- **ShoppingListGenerator** — aggregate planned ingredients − pantry; preferred store; combine list;
  flag shared/bulk; sum cost vs budget; mark estimates.
- **DealDetector** — current store price vs historical average → deal flag.
- **PrepTimeLoad** — per-day prep + cook minutes for busy-day awareness.

## Pages

1. **Dashboard** — nutrition per person vs goals; budget vs projected spend; prep load; alerts.
2. **People & Goals** — CRUD two people + goals.
3. **Ingredients** — CRUD; manual nutrition or CNF search-to-populate; estimate flags.
4. **Prices** — per ingredient × store price history; preferred store; deal indicators.
5. **Recipes / Meals** — CRUD; ingredient lines; auto nutrition + cost per serving; times.
6. **Pantry / Freezer** — inventory on hand.
7. **Meal Planner** — monthly calendar (weekly rows); assign recipes per person/shared; day flags;
   per-day prep load; per-day/person nutrition rollup.
8. **Shopping List** — generated per plan − pantry; combined with preferred store; totals vs budget;
   shared/bulk highlights; estimate + deal badges.
9. **Settings** — monthly budget, stores, CNF import trigger/status.

## Phased delivery

- **Phase 0 — Scaffold + Aspire** *(complete)*: solution + all projects, MudBlazor earth-tones
  theme, EF Core + SQLite (Api-owned), service discovery Web → Api, backup-then-`Migrate()` on
  startup, base layout/nav, architecture docs.
- **Phase 1 — Core data**: People/Goals, Stores, Ingredients, Prices (Domain, Data + migration,
  Contracts, Api, Web CRUD).
- **Phase 2 — CNF import + nutrition lookup**.
- **Phase 3 — Recipes + NutritionCalculator + CostCalculator**.
- **Phase 4 — Pantry/Freezer**.
- **Phase 5 — Meal Planner** (calendar, PlanAggregator, PrepTimeLoad).
- **Phase 6 — Shopping List + budget + DealDetector + bulk**.
- **Phase 7 — Dashboard aggregations**.
- **Phase 8 — Docker finalize** (volumes, WAL, backup job) + README + seed data.

## Verification

- NUnit unit tests for all Domain calculators (target high Domain coverage).
- `dotnet build` + `dotnet test` clean.
- `aspire run` → dashboard shows Api + Web healthy; Web reaches Api via service discovery.
- `aspire publish` → Docker Compose; `docker compose up` → reachable on LAN; SQLite persists across
  restarts.
