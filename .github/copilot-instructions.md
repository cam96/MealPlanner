# MealPlanner — Copilot project instructions

Project-wide standards for all current and future work in this repository. These apply to every
contribution unless a more specific instruction file (for example
[.github/instructions/blazor.instructions.md](instructions/blazor.instructions.md)) overrides them.

## What this project is

A local-network **meal-planning web app** for a two-person household in Winnipeg. It plans monthly
meals (weekly breakdown) to hit per-person nutrition goals, control a grocery budget, and respect
schedules. It runs in Docker on the home network. See [specs/meal-planner-plan.md](../specs/meal-planner-plan.md)
and [specs/architecture.md](../specs/architecture.md).

## Architecture (do not violate the layering)

Orchestrated by **.NET Aspire**. Two runtime services: **Api** (backend) and **Web** (UI).

| Project | Responsibility | May depend on |
| --- | --- | --- |
| `MealPlanner.Domain` | Entities + calculators. **Pure C#, no EF/ASP.NET/HTTP/IO.** | — |
| `MealPlanner.Data` | EF Core `DbContext`, entity configs, migrations, CNF importer. | Domain |
| `MealPlanner.Contracts` | DTOs shared over HTTP. | — |
| `MealPlanner.Api` | Web API endpoints, validation, composition root, startup migration. | Domain, Data, Contracts, ServiceDefaults |
| `MealPlanner.Web` | Blazor + MudBlazor UI. **No EF, no direct DB access.** | Contracts, ServiceDefaults |
| `MealPlanner.ServiceDefaults` | OTel, health, resilience, service discovery. | — |
| `MealPlanner.AppHost` | Aspire orchestration. | Api, Web (as resources) |

Hard rules:

- **Only the Api touches the database.** The Web project calls the Api through the typed
  `MealPlannerApiClient` whose base address is `https+http://api` (Aspire service discovery).
- **Domain stays pure** — no framework or I/O dependencies; it must be unit-testable in isolation.
- Data flows in one direction: `Web → Api → (Domain + Data) → SQLite`. DTOs in `Contracts` are the
  only shapes crossing the HTTP boundary.

## Tech & tooling

- **.NET 10** (LTS), C# latest. Nullable + implicit usings enabled solution-wide.
- **EF Core 10 + SQLite**. **MudBlazor** for UI. **NUnit** for tests.
- **Central Package Management**: declare every package version in `Directory.Packages.props`.
  Never put a `Version=` on a `PackageReference` in a `.csproj`.
- Common build settings live in `Directory.Build.props` (`TreatWarningsAsErrors=true`). NuGet audit
  advisories `NU1901–NU1904` are demoted to warnings there because current fixes are incompatible
  major bumps — revisit when Microsoft ships patched transitive dependencies.

## Coding conventions

- File-scoped namespaces; `System.*` usings first; interfaces prefixed with `I`.
- Public types and members get **XML doc comments**.
- Validate at boundaries with guard clauses: `ArgumentNullException.ThrowIfNull`,
  `ArgumentException.ThrowIfNullOrWhiteSpace`. Do not add defensive checks for states that cannot
  occur.
- Prefer `async`/`await` end-to-end for I/O; suffix async methods with `Async`; accept a
  `CancellationToken` on public async APIs.
- Units are **grams (g)** and **millilitres (ml)** only; counts convert via a per-ingredient
  serving weight.
- Anything the app estimates (nutrition or cost) must carry an **estimate flag** and be visibly
  marked in the UI.

## Data & migrations (data must never be lost)

- Use **EF Core migrations** only — never `EnsureCreated` in app code.
- Startup sequence (in the Api): ensure the DB directory exists → **back up the DB file if there
  are pending migrations** → `Database.Migrate()` → enable **WAL**. Implemented in
  `MealPlanner.Data.DatabaseMaintenance`.
- The SQLite file and backups live on **Docker volumes** (`/data`, `/backups`), never the container
  layer.
- Destructive schema changes use **expand/contract** across multiple migrations.

## Blazor / UI

- Follow [.github/instructions/blazor.instructions.md](instructions/blazor.instructions.md).
- Use the shared **earth-tones** theme in `MealPlanner.Web.Theme.EarthTonesTheme`; do not hard-code
  colors in components.
- Use MudBlazor components (data grids, date pickers, autocomplete, dialogs, form validation) rather
  than hand-rolled markup.

## Testing

- Unit-test all Domain calculators; target high Domain coverage.
- NUnit 4 assertions; test argument-validation guards for exact exception types.
- Keep `dotnet build` and `dotnet test` clean before finishing a change.

## After generating code

- Update [README.md](../README.md) to reflect new features, setup, or run steps.
- Keep [specs/architecture.md](../specs/architecture.md) current when the architecture changes.

## Skills to honor when relevant

`aspire`, `csharp-async`, `csharp-docs`, `dotnet-best-practices`, `dotnet-design-pattern-review`,
`ef-core`, `nunit-argument-validation`, `dotnet-code-coverage`.
