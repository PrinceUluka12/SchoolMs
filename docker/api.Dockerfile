# ── Build stage ────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (layer cache optimisation)
COPY SchoolMS.slnx .
COPY SchoolMS.Api/SchoolMS.Api.csproj                         SchoolMS.Api/
COPY SchoolMS.Application/SchoolMS.Application.csproj         SchoolMS.Application/
COPY SchoolMS.Core/SchoolMS.Core.csproj                       SchoolMS.Core/
COPY SchoolMS.Infrastructure/SchoolMS.Infrastructure.csproj   SchoolMS.Infrastructure/

RUN dotnet restore SchoolMS.slnx

# Copy all source files
COPY . .

RUN dotnet publish SchoolMS.Api/SchoolMS.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for health check
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p /app/uploads

# Non-root user for security
RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "SchoolMS.Api.dll"]