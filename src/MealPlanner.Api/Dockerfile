# MealPlanner API — multi-stage build. Build context is the repository root so shared
# Directory.Build.props / Directory.Packages.props are available during restore.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG VERSION=0.0.0-dev

COPY . .
RUN dotnet restore src/MealPlanner.Api/MealPlanner.Api.csproj
RUN dotnet publish src/MealPlanner.Api/MealPlanner.Api.csproj -c Release -o /app --no-restore \
    /p:Version=${VERSION}

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

LABEL org.opencontainers.image.title="MealPlanner API" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.source="https://github.com/cam96/MealPlanner"

# curl is used by the container health check; the data/backups directories are created up front
# and owned by the non-root app user so the named volumes inherit writable ownership.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data /data/cnf /backups \
    && chown app:app /data /data/cnf /backups

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "MealPlanner.Api.dll"]
