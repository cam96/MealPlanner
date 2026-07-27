# Acceptance Test Findings

End-to-end acceptance test performed against the MealPlanner web app, acting as a household user
and exercising every page in the integrated browser. This document records design observations and
defects for resolution. It does **not** change any application code.

> **Run 2 (2026-07-26): full green re-test.** With the render-mode fix from Run 1 in place, every
> page and workflow was exercised end to end by creating real data through the UI (person, CNF-backed
> ingredient, price, recipe, planned meal, pantry stock, budget). All interactions work. One new
> low-severity finding was recorded and **fixed** (Issue 4 — search "no results" reused the "no data"
> empty state). See the [Run 2 section](#run-2--full-workflow-re-test-2026-07-26) below.

## Test environment

- **Date:** 2026-07-26
- **API:** `http://localhost:5191` (run via `dotnet run --project src/MealPlanner.Api`)
- **Web:** `http://localhost:5100` (run via `dotnet run --project src/MealPlanner.Web --no-launch-profile`)
  with service discovery `services__api__http__0=http://localhost:5191`
- **Database:** fresh SQLite (migrations applied, WAL enabled). Demo data **not** seeded; the three
  default stores (Costco, Safeway, Superstore) come from an EF migration `HasData` seed.
- **Method:** navigated every page, reviewed layout/copy, and attempted the primary interactive
  actions (add/edit/delete dialogs, search boxes, grid sorting/filtering, drawer toggle,
  navigation buttons).

## Result summary

| Area | Status |
| --- | --- |
| Page routing / navigation links | ✅ Working |
| Static rendering & empty states of every page | ✅ Working |
| Layout, theme, copy | ✅ Good (see minor notes) |
| **All interactive behaviour** (dialogs, buttons, search, sort/filter, forms, drawer) | ❌ **Broken** |

Every page renders correctly, but no interactive feature works because the application is not
configured with an interactive render mode (see Issue 1). This single defect blocks essentially all
user actions, so most per-page checks below are blocked rather than individually failing.

> **Update (resolved 2026-07-26):** All three issues below have been fixed and re-verified in the
> browser. Enabling `InteractiveServer` rendering restored the whole UI: the add/edit/delete
> dialogs, forms, snackbars, grids and navigation now work end to end (verified by adding then
> deleting a person). The Prices empty-state hint and the People empty-state rendering were also
> confirmed.

---

## Issue 1 — CRITICAL: No interactive render mode; the entire app is static

**Status:** ✅ Fixed — `@rendermode="InteractiveServer"` added to `<HeadOutlet />` and `<Routes />`
in [App.razor](../src/MealPlanner.Web/Components/App.razor). Re-verified: "Add person" opens its
dialog, Save persists via the API and refreshes the grid with a snackbar, and Delete works.

**Severity:** Critical (blocks all user actions)

**Symptom:** Clicking any button, opening any dialog, using any search box, sorting/filtering any
data grid, toggling the navigation drawer, and submitting any form all do nothing. Verified
concretely that "Add person" (People), "Add recipe" (Recipes), and the toolbar hamburger toggle
produce no effect.

**Root cause:** The Blazor Web App never opts into an interactive render mode.

- [src/MealPlanner.Web/Components/App.razor](../src/MealPlanner.Web/Components/App.razor) renders
  `<Routes />` and `<HeadOutlet />` with no `@rendermode`.
- No page or component declares `@rendermode InteractiveServer` (a repository-wide search finds
  only the `@using static ...RenderMode` import in `_Imports.razor`).
- [src/MealPlanner.Web/Program.cs](../src/MealPlanner.Web/Program.cs) does call
  `AddInteractiveServerComponents()` and `AddInteractiveServerRenderMode()`, which **enables** the
  mode but does not **apply** it to any component.

Because Blazor Web Apps default to static server-side rendering, the whole UI is served as static
HTML with enhanced navigation. MudBlazor dialogs, snackbars, autocompletes, data-grid interactions,
`OnClick` handlers, and `NavigationManager.NavigateTo` calls in handlers are therefore inert.

**Suggested fix:** Enable global interactive server rendering in
[App.razor](../src/MealPlanner.Web/Components/App.razor):

```razor
<HeadOutlet @rendermode="InteractiveServer" />
...
<Routes @rendermode="InteractiveServer" />
```

(Or apply `@rendermode InteractiveServer` per page/component if global interactivity is not desired.)
After the fix, re-run this acceptance test — the checks below are currently **blocked** by this
issue and should be re-verified.

---

## Issue 2 — MINOR: People data grid overflows / empty-state text truncated

**Status:** ✅ Not reproducing after the Issue 1 fix — the "No people yet…" empty-state message now
renders in full at a normal viewport. The remaining horizontal scroll on very narrow widths is
MudDataGrid's built-in behaviour and is acceptable; no code change made.

**Severity:** Low (cosmetic)

**Page:** [People.razor](../src/MealPlanner.Web/Components/Pages/People.razor)

**Symptom:** On a standard viewport the People grid (Name, Calories, Protein, Fibre, Carbs, Fat,
Actions) is wider than its container, producing a horizontal scrollbar and clipping the
"No people yet…" empty-state message ("…you're planning meals" is cut off).

**Suggestion:** Allow the grid to wrap/condense on narrow widths, reduce column header padding, or
abbreviate headers so the seven columns fit without horizontal scroll.

---

## Per-page rubberduck notes (design review)

These observations assume Issue 1 is fixed so the interactive elements actually function.

### Dashboard (`/`)
- Clear three-panel layout: Nutrition (avg/day), Budget, Cooking load, with a month selector and
  sensible empty states ("No normal days are planned this month yet.").
- Budget panel helpfully links to Settings when no budget is set. Good.
- Estimate-asterisk convention is explained in the intro copy — consistent with the project rule
  that estimated figures must be visibly marked.

### People & Goals (`/people`)
- Add/edit/delete via dialog; goals shown per person. See Issue 2 for the grid overflow.

### Ingredients (`/ingredients`)
- Search box + grid with sort/filter; per-100 g/ml nutrition with estimate flagging described in
  copy. All actions blocked by Issue 1.

### Prices (`/prices`)
- Renders an ingredient autocomplete first; the price grid appears after selecting an ingredient.
  ✅ Fixed: a "Pick an ingredient above to see or record its prices." hint now shows when nothing is
  selected so the page no longer looks half-rendered.
- Selecting an ingredient and recording a price works now that Issue 1 is fixed.

### Recipes (`/recipes`) and Recipe editor (`/recipes/new`, `/recipes/{id}`)
- List has search + grid; "Add recipe" is an `OnClick` that calls `NavigationManager.NavigateTo`,
  so it is blocked by Issue 1 (the button does nothing). The editor page itself renders correctly
  when reached directly by URL, with name, meal type, servings/prep/cook steppers, instructions,
  an ingredients builder, and a live "Per serving" panel.
- Consideration: because "Add recipe" relies on interactivity, even navigation to the editor is
  currently impossible from the UI. After Issue 1 is fixed this works; alternatively the button
  could be a plain link (`<MudButton Href="/recipes/new">`) so it works without interactivity.

### Pantry & Freezer (`/pantry`)
- Search + grid (Location, Ingredient, On hand, Actions) with a good empty state. Actions blocked by
  Issue 1.

### Meal Planner (`/planner`)
- Monthly calendar grid renders correctly with weekday headers, day numbers, per-day "Meal" and
  day-type buttons, month navigation and a "Today" button. Adding/editing meals and changing day
  types is blocked by Issue 1.

### Shopping List (`/shopping-list`)
- Month selector, estimated total vs. monthly budget, and a clear empty state. Cost estimate uses
  the preferred store and is asterisk-flagged per project convention. Generation depends on planned
  meals, which are unreachable until Issue 1 is fixed.

### Settings (`/settings`)
- Monthly budget stepper + "Save budget", and a Stores section (Add store, edit/delete rows) showing
  the three seeded stores. Saving the budget and managing stores are blocked by Issue 1.

---

## Recommended next steps

1. ✅ Done — Issue 1 fixed (`InteractiveServer` render mode enabled), which unblocked the whole app.
2. ✅ Done — re-verified interactivity end to end (add/edit/delete person, dialogs, snackbars).
3. ✅ Done — Prices empty-state hint added; People empty-state no longer truncates.
4. ✅ Done (Run 2) — exercised the remaining page workflows (recipes, planner, shopping list) with
   real data. All calculators and cross-page flows verified. See Run 2 below.
5. ✅ Done — Issue 4 fixed: searchable grids (Ingredients, Recipes, Pantry) now show "No … match
   your search." when a filter is active, keeping the "add your first…" copy only for a truly empty
   data set. Optionally, "Add recipe" could still be made a plain link
   (`<MudButton Href="/recipes/new">`) for resilience if interactivity is ever lost.

---

# Run 2 — full workflow re-test (2026-07-26)

Second pass performed after the Run 1 render-mode fix. This run built a complete, realistic data set
through the UI and validated every page's primary workflow plus the cross-page calculations
(nutrition, cost, pantry deduction, budget).

## Environment

- **API:** `http://localhost:5191` (`dotnet run --project src/MealPlanner.Api`, Development).
- **Web:** `http://localhost:5100` (`--no-launch-profile`, `services__api__http__0=http://localhost:5191`).
- **Database:** fresh SQLite, migrations applied, WAL enabled. Three stores (Costco/Safeway/
  Superstore) from the `HasData` seed. All other data created live through the UI during the test.
- **Render mode:** `InteractiveServer` confirmed active in
  [App.razor](../src/MealPlanner.Web/Components/App.razor) — the full UI is interactive.

## Result summary

| Area | Status |
| --- | --- |
| Navigation drawer + routing | ✅ Working |
| People — add (dialog, defaults, snackbar, grid refresh) | ✅ Working |
| Ingredients — add + **CNF autocomplete** (populates nutrition + source citation) | ✅ Working |
| Ingredients — search filter | ✅ Working (see Issue 4 for empty-result copy) |
| Prices — ingredient autocomplete, record price, preferred flag | ✅ Working |
| Recipes — add, editor, ingredient builder, **per-serving nutrition + cost calc** | ✅ Working |
| Pantry — add item, group-by-location | ✅ Working |
| Meal Planner — add meal, per-person goal progress, edit-day dialog | ✅ Working |
| Shopping List — generation, **pantry deduction**, package/bulk rounding, cost | ✅ Working |
| Settings — save budget, stores list | ✅ Working |
| Dashboard — nutrition averages, budget vs. spend, cooking load | ✅ Working |
| Estimate-asterisk convention | ✅ Consistent (copy explains it on every relevant page) |

**No blocking or high-severity defects found in Run 2.** One low-severity UX finding (Issue 4),
now fixed.

## Data created during the test (proves the end-to-end path)

- **Person:** `Alex` with default goals (2000 kcal / 100 P / 30 Fib / 250 C / 70 F).
- **Ingredient:** `Deli-meat, chicken breast, cooked, extra lean` populated from the **Canadian
  Nutrient File** (food code 1220 → 96 kcal, 17.14 g protein, 1.78 g carbs, 1.79 g fat per 100 g,
  with the source citation shown in the dialog).
- **Price:** Costco, `$12.99` for a `500 g` package, marked **preferred**.
- **Recipe:** `Chicken Bowl`, 2 servings, 300 g chicken. Per-serving panel calculated **144 kcal /
  25.7 g protein** and **$3.90/serving ($7.79 total)** — both arithmetically correct against the CNF
  and price data.
- **Planned meal:** Chicken Bowl (Dinner, shared, 2 servings) on July 1 → day cell showed
  `Alex: 288 / 2000 kcal` (2 × 144, correct).
- **Pantry:** 200 g chicken in Pantry.
- **Budget:** `$600.00` monthly.

## Cross-cutting checks that passed

- **Cost engine:** shopping list picked the preferred store (Costco), rounded 100 g up to one 500 g
  package = `$12.99`, and flagged it **Bulk**.
- **Pantry deduction:** after adding 200 g to the pantry, the shopping list "To buy" dropped from
  `300 g` to `100 g` — the "minus what's already in your pantry/freezer" logic works.
- **Budget rollup:** dashboard showed projected spend `$12.99` against the `$600` budget with
  `$587.01 remaining`, matching the shopping-list total.
- **Estimate flagging:** every page that shows estimated nutrition/cost explains the asterisk
  convention in its intro copy; non-estimated figures (CNF nutrition, an exact recorded price) were
  correctly shown without an asterisk.

## Issue 4 — LOW: grid "no search results" reuses the "no data yet" empty state

**Status:** ✅ Fixed — the searchable grids now show a search-aware message when a filter is active
but the underlying data set is non-empty. Verified in the browser (searching `zzzznotfound` on
Ingredients now shows "No ingredients match your search."). Applied to
[Ingredients.razor](../src/MealPlanner.Web/Components/Pages/Ingredients.razor),
[Recipes.razor](../src/MealPlanner.Web/Components/Pages/Recipes.razor), and
[Pantry.razor](../src/MealPlanner.Web/Components/Pages/Pantry.razor).

**Severity:** Low (cosmetic/UX copy)

**Page:** [Ingredients.razor](../src/MealPlanner.Web/Components/Pages/Ingredients.razor) (and likely
the other searchable grids — Recipes, Pantry — which share the pattern).

**Symptom:** With one ingredient present, typing a non-matching search term (`zzzznotfound`) filters
the grid to zero rows and displays **"No ingredients yet. Add the foods you cook with."** — the same
message shown when the table is genuinely empty. This is misleading: ingredients *do* exist; none
match the search.

**Suggested fix:** Show a search-aware empty message when a filter/search is active, e.g. "No
ingredients match your search." (and similarly "No recipes match your search." / "No pantry items
match your search.") while keeping the "add your first…" copy only when the underlying data set is
empty.

## Per-page rubberduck notes (Run 2)

- **Dashboard** — Now data-driven: per-person macro progress bars, budget progress with "remaining"
  copy, cooking-load summary. Monthly averages spread a single meal across all 31 days
  (`9 kcal/day`); this is by design ("average per day") but is worth a tooltip if users find it
  surprising. Not a defect.
- **People** — Add dialog has sensible default goals; snackbar + grid refresh on save.
- **Ingredients** — CNF autocomplete is the standout feature and works smoothly (debounced search,
  real results, auto-fills nutrition + attributes the source). Estimate switch present.
- **Prices** — Autocomplete → per-ingredient grid; record-price dialog defaults date to today and
  pre-selects a seeded store; preferred/estimated toggles both function.
- **Recipes / editor** — "Add recipe" navigates to `/recipes/new`; ingredient builder adds lines
  with ingredient autocomplete + qty + unit; the "Per serving" panel recalculates on save and shows
  total cost. Meal-type and step fields all work.
- **Pantry** — Add-item dialog with ingredient autocomplete, qty/unit, location; rows group by
  location.
- **Meal Planner** — Calendar renders the month; "Meal" opens an add-meal dialog (slot / for /
  recipe / servings); day cells show per-person goal progress; the day-number button opens an
  "Edit day" dialog (day type + note).
- **Shopping List** — Aggregates the month, deducts pantry, groups by store, rounds to packages, and
  flags bulk buys; header shows estimated total vs. budget.
- **Settings** — Budget stepper + save (snackbar, button disables when unchanged); stores table lists
  the three seeded stores with edit/delete actions.

## Notes for future browser runs

- Nav-drawer links can sit outside the viewport after a snackbar; navigating by URL
  (`navigate_page`) is more reliable than clicking the drawer link.
- The interactive circuit re-renders element `ref`s on navigation; a click may need to target the
  freshly-issued `ref` (re-read the page, then click).

---

## Run 3 — Meal Categories & Combos feature test (2026-07-27)

Focused acceptance test of the new **food categorization** feature: categorizing ingredients into
Protein / Carbohydrate / Vegetable, the three-column category board with pantry stock, and informal
**meal combos** (1 protein + 1 carb + 1 vegetable) that can be saved and dropped onto the planner.
Driven through the integrated browser as a household user. **No application code was changed** during
this run.

### Test environment

- **Date:** 2026-07-27
- **API:** `http://localhost:5191` (`ASPNETCORE_ENVIRONMENT=Development`, `MealPlanner__SeedDemoData=true`)
- **Web:** `http://localhost:5100` (`--no-launch-profile`, `services__api__http__0=http://localhost:5191`)
- **Database:** existing SQLite from a prior run. The new EF migration
  `AddFoodCategoriesAndCombos` applied cleanly at API startup, with a pre-migration backup written to
  `data/backups`. Because the demo seeder is idempotent, it short-circuited on the pre-populated DB;
  test data was created through the UI/API instead:
  - `Deli-meat, chicken breast…` → **Protein** (200 g in Pantry)
  - `Brown rice, cooked` → **Carbohydrate** (not on hand)
  - `Broccoli, cooked` → **Vegetable** (not on hand)
  - Combo **"Chicken, rice & broccoli"** built from the three above.
- **Method:** navigated every relevant page and exercised the primary interactive flows
  (categorize dialog, build-a-combo dialog + validation, add-combo-to-planner, delete-protection,
  ingredient edit dialog).

### Result summary

| Area | Status |
| --- | --- |
| `Meal Categories` nav link & page routing | ✅ Working |
| Three-column board (Protein/Carb/Vegetable) — icons, colors, earth-tones theme | ✅ Working |
| Assign-to-category dialog (autocomplete filter, clear, select) | ✅ Working |
| Category assignment persists | ✅ Working |
| Pantry stock shown beside categorized ingredients (`Pantry: 200 g` / `Not on hand`) | ✅ Working |
| Build-a-combo dialog (name + 3 selects) | ✅ Working |
| Combo validation (name required + ≥1 ingredient) blocks save | ✅ Working |
| Combo saved → card renders with Protein/Carb/Veg lines + Edit/Delete | ✅ Working |
| Add combo to planner (Recipe / Meal-combo radio toggle + autocomplete) | ✅ Working |
| Combo shows on planner day (`D: Chicken, rice & broccoli (shared)`) | ✅ Working |
| Delete-protection: in-use combo can't be deleted (warning snackbar) | ✅ Working |
| Ingredient edit dialog exposes **Meal category** select, correctly populated | ✅ Working |
| Migration applied at startup with pre-migration backup | ✅ Working |

Overall: the feature works end to end. All findings below are **low severity / by-design nuances** —
no functional defects were found.

### Per-flow notes

- **Category board** renders three columns with distinct icons and earth-tone colors. Empty columns
  and empty stock show clear states (`Not on hand`). Chicken correctly showed `Pantry: 200 g`.
- **Assign-to-category dialog** autocomplete filtered on typing (`chicken` → the chicken option);
  the clear button works; selecting an item and confirming re-categorizes it and refreshes the board.
- **Build-a-combo dialog** copy is clear. Submitting with an empty name and no ingredients surfaced
  both `Name is required.` and `Choose at least one ingredient.` and blocked the save, as expected.
- **Planner integration** — the Add-meal dialog's Recipe / Meal-combo radio toggle swaps the picker;
  the combo autocomplete found the saved combo; the planned day cell then showed
  `D: Chicken, rice & broccoli (shared)`.
- **Delete-protection** — deleting the in-use combo produced the warning snackbar
  `This combo is used in a meal plan and can't be deleted.` and the card remained. (Backed by the FK
  `Restrict` on `PlannedMeals → MealCombos`, also covered by a unit test.)
- **Ingredient edit dialog** — the **Meal category** select is present with helper text
  "Group interchangeable foods for building meals." and was correctly pre-populated (`Vegetable` for
  Broccoli).

### Observations (low severity / by-design)

1. **[Low] Combo-only planner days show no per-person nutrition line.** A day whose only meal is a
   combo shows no `Alex: … kcal` line (unlike recipe days), because combos intentionally carry no
   quantities/nutrition. This is by design, but a planned dinner silently contributing 0 kcal could
   confuse a user. *Suggestion:* show a subtle hint on combo meals (e.g. "combo — no nutrition
   estimate") so the absence is explained rather than looking like missing data.

2. **[Low] Ingredients data grid has no Category column.** An ingredient's meal category is only
   visible in its edit dialog and on the category board, not in the main ingredients grid.
   *Suggestion:* surface the category as a column or chip in the grid for discoverability.

3. **[Info/Low] Assign-to-category dialog can move an ingredient already in another category.**
   `Categories.razor` builds the candidate list with `i.Category != category`, so an ingredient
   currently assigned to a *different* category still appears and, if picked, is moved. The behavior
   is reasonable (one category per ingredient = moving between columns), but:
   - `AssignCategoryDialog`'s XML doc comment says "already-categorised ones are excluded", which is
     inaccurate — only items already in *this* category are excluded.
   - The picker gives no visual cue that an item is currently in another column, so a user could move
     it unexpectedly. *Suggestion:* fix the doc comment and optionally annotate items with their
     current category in the autocomplete.

4. **[Info] Demo seeder is idempotent.** On a pre-populated DB the seeder skips, so the new demo
   category assignments/combo don't appear. Expected; only relevant on a fresh database.

### Suggested follow-ups (optional, not blocking)

- Fix the misleading `AssignCategoryDialog` doc comment (Observation 3).
- Consider a "combo — no nutrition" hint on planner combo meals (Observation 1).
- Consider a Category column/chip in the ingredients grid (Observation 2).

