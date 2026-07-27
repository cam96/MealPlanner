# MealPlanner Web (Blazor) — multi-stage build. Build context is the repository root so shared
# Directory.Build.props / Directory.Packages.props are available during restore.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore src/MealPlanner.Web/MealPlanner.Web.csproj
RUN dotnet publish src/MealPlanner.Web/MealPlanner.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "MealPlanner.Web.dll"]
