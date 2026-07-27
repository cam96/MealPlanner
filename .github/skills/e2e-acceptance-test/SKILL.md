---
name: e2e-acceptance-test
description: 'Run an end-to-end acceptance test of the MealPlanner web UI in the integrated browser, acting as a real user. Use when asked to acceptance-test, smoke-test, click through, rubberduck, or manually verify pages/buttons/searches/dialogs across the app, and to log findings to specs/.'
---

# End-to-End Acceptance Testing (browser MCP)

Drive the running MealPlanner web app in the **integrated browser** as if you were a household user:
visit every page, exercise buttons/searches/dialogs/forms, review design, and record defects in a
`specs/` findings document. **Document issues — do not fix code** unless the user explicitly asks.

## Architecture reminder

Data flows `Web → Api → (Domain + Data) → SQLite`. The Web front-end reaches the Api only through
Aspire service discovery (`https+http://api`). So the Api **and** Web must both run, and the Web
must be told where the Api is.

## 1. Build and run both services

```pwsh
dotnet build MealPlanner.slnx --nologo -clp:ErrorsOnly
```

Run the two services as background (async) processes with **fixed http ports** and manual service
discovery — simpler and more controllable than launching the Aspire AppHost (whose resource ports
are dynamic).

```pwsh
# API — note: launchSettings.json overrides ASPNETCORE_URLS, so read the actual port from the
# "Now listening on:" log line (typically http://localhost:5191).
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run --project src/MealPlanner.Api --no-build
```

```pwsh
# Web — bypass its launch profile and point service discovery at the API's real port.
$env:ASPNETCORE_URLS="http://localhost:5100"
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:services__api__http__0="http://localhost:5191"   # match the API's actual port
dotnet run --project src/MealPlanner.Web --no-build --no-launch-profile
```

Notes:
- Use **http** everywhere to avoid dev-cert warnings in the integrated browser. Providing only an
  http service-discovery endpoint makes `https+http://api` resolve over http, and binding the Web to
  http-only skips HTTPS redirection.
- The DB starts empty; the three stores (Costco/Safeway/Superstore) are seeded by an EF migration
  `HasData`. To get richer data either create it through the UI (truest acceptance test) or set
  `MealPlanner:SeedDemoData=true` on the API.

## 2. Drive the browser

Use the browser MCP tools: `open_browser_page`, `read_page`, `click_element`, `type_in_page`,
`navigate_page`, `screenshot_page`, `handle_dialog`, `drag_element`.

- Open `http://localhost:5100/` and work through the nav: Dashboard, People, Ingredients, Prices,
  Recipes, Pantry, Meal Planner, Shopping List, Settings (plus `/recipes/new`).
- `read_page` returns an accessibility snapshot with element `ref`s — click/type by `ref`.
- For each page, verify it renders, then attempt its primary actions: add/edit/delete dialogs,
  search boxes, data-grid sort/filter, forms, month navigation, and navigation buttons.
- Take a `screenshot_page` when reviewing visual/design details.

## 3. Rubberduck each page

For every page ask: Does it load without server errors? Are empty states clear? Do buttons do what
they say? Do searches filter? Are estimated figures asterisk-flagged (project rule)? Is copy/layout
coherent? Note anything confusing or broken.

## 4. Known failure mode to check first

If **every** button/dialog/search is inert, the app is almost certainly rendering as **static SSR**
with no interactive render mode. Confirm with a repo search for `@rendermode` / `InteractiveServer`:
Blazor Web Apps default to static, so `App.razor` needs `@rendermode="InteractiveServer"` on
`<Routes />` (and `<HeadOutlet />`), or per-page `@rendermode InteractiveServer`. `OnClick`-based
navigation (`NavigationManager.NavigateTo`) also silently fails under static rendering.

## 5. Record findings

Write results to `specs/acceptance-test-findings.md` (or a dated variant). Include: test environment
(ports, seed state), a result summary table, each issue with **severity / page / symptom / root
cause / suggested fix** (link files with workspace-relative paths), per-page rubberduck notes, and
recommended next steps. If a single defect blocks most interactions, say so and mark dependent
checks as *blocked* rather than individually failed.

## 6. Clean up

`kill_terminal` the API and Web background terminals when finished.
