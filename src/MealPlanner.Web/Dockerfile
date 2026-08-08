# MealPlanner Web (Blazor) — multi-stage build. Build context is the repository root so shared
# Directory.Build.props / Directory.Packages.props are available during restore.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG VERSION=0.0.0-dev

COPY . .
RUN dotnet restore src/MealPlanner.Web/MealPlanner.Web.csproj
RUN dotnet publish src/MealPlanner.Web/MealPlanner.Web.csproj -c Release -o /app --no-restore \
    /p:Version=${VERSION}

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

LABEL org.opencontainers.image.title="MealPlanner Web" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.source="https://github.com/cam96/MealPlanner"

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "MealPlanner.Web.dll"]
