var builder = DistributedApplication.CreateBuilder(args);

// The API owns the SQLite database and is the reusable backend.
var api = builder.AddProject<Projects.MealPlanner_Api>("api");

if (builder.ExecutionContext.IsPublishMode)
{
    // In the published (containerised) topology the database and backups live under /data and
    // /backups. The matching named volumes are declared on the generated Docker Compose service
    // during publish (see specs/architecture.md and the Phase 8 deployment steps).
    api.WithEnvironment("ConnectionStrings__mealplanner", "Data Source=/data/mealplanner.db")
       .WithEnvironment("MealPlanner__BackupDirectory", "/backups")
       .WithEnvironment("MealPlanner__CnfDirectory", "/data/cnf");
}

// The Blazor front-end calls the API via Aspire service discovery (never the database directly).
builder.AddProject<Projects.MealPlanner_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
