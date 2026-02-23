# ─────────────────────────────────────────────────────────────────────────────
# FemVed API — Multi-stage Dockerfile
# Stage 1 (build): Restore & publish using the .NET 10 SDK
# Stage 2 (runtime): Lean ASP.NET 10 runtime image
# ─────────────────────────────────────────────────────────────────────────────

# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first (layer-cache friendly — only re-runs restore when
# .csproj files change, not on every source code change)
COPY src/FemVed.Domain/FemVed.Domain.csproj             src/FemVed.Domain/
COPY src/FemVed.Application/FemVed.Application.csproj   src/FemVed.Application/
COPY src/FemVed.Infrastructure/FemVed.Infrastructure.csproj src/FemVed.Infrastructure/
COPY src/FemVed.API/FemVed.API.csproj                   src/FemVed.API/

# Restore dependencies
RUN dotnet restore src/FemVed.API/FemVed.API.csproj

# Copy the full source (after restore so the layer above is cached)
COPY src/ src/

# Publish a self-contained Release build to /app/publish
RUN dotnet publish src/FemVed.API/FemVed.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Railway routes external HTTPS traffic to the container on port 8080 via HTTP.
# The app must listen on 0.0.0.0:8080 (not localhost).
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FemVed.API.dll"]
