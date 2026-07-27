using System.Text.Json.Serialization;
using MealPlanner.Api.Endpoints;
using MealPlanner.Data;

var builder = WebApplication.CreateBuilder(args);

// Aspire cross-cutting concerns: OpenTelemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

// Data layer (SQLite). Only the API touches the database.
var connectionString = builder.Configuration.GetConnectionString("mealplanner")
    ?? "Data Source=data/mealplanner.db";
builder.Services.AddMealPlannerData(connectionString);

// Canadian Nutrient File (CNF) lookup for populating ingredient nutrition (read locally).
builder.Services.AddCnfFoodLookup(builder.Configuration["MealPlanner:CnfDirectory"] ?? "data/cnf");

// API surface.
builder.Services.AddOpenApi();

// Serialize enums as their string names on the wire for readable, stable payloads.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Back up and migrate the database on startup (backup-first, then apply pending migrations).
await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MealPlannerDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var backupDirectory = builder.Configuration["MealPlanner:BackupDirectory"];
    await DatabaseMaintenance.BackupThenMigrateAsync(context, backupDirectory, logger);

    // Optionally seed representative demo data on a fresh install (off by default).
    if (builder.Configuration.GetValue<bool>("MealPlanner:SeedDemoData"))
    {
        await DataSeeder.SeedDemoDataAsync(context, logger);
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Aspire default endpoints (health/liveness).
app.MapDefaultEndpoints();

// Lightweight readiness ping used by the Web front-end to verify service discovery end-to-end.
app.MapGet("/ping", () => Results.Ok(new { service = "MealPlanner.Api", status = "ok" }))
    .WithName("Ping");

// Core data CRUD endpoints (Phase 1).
app.MapPeopleEndpoints();
app.MapStoresEndpoints();
app.MapIngredientsEndpoints();
app.MapPricesEndpoints();

// Recipes with computed nutrition and cost (Phase 2).
app.MapRecipesEndpoints();

// Pantry and freezer inventory (Phase 4).
app.MapPantryEndpoints();

// Monthly meal planning with per-person nutrition rollups (Phase 5).
app.MapPlannerEndpoints();

// Meal-building categories board and informal meal combos.
app.MapCombosEndpoints();

// Shopping list generation and household settings (Phase 6).
app.MapSettingsEndpoints();
app.MapShoppingEndpoints();

// At-a-glance monthly dashboard: nutrition, budget, prep load and alerts (Phase 7).
app.MapDashboardEndpoints();

// Canadian Nutrient File search-to-populate for ingredient nutrition.
app.MapCnfEndpoints();

app.Run();
