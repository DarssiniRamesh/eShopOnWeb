# syntax=docker/dockerfile:1.4
# Multi-stage Dockerfile for eShopOnWeb that explicitly targets the correct solution file
# to avoid MSBUILD ambiguity when multiple solutions/projects exist in the folder.

# =========================
# BASE BUILD IMAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy sln and props first to leverage Docker layer caching
COPY eShopOnWeb.sln ./
COPY Directory.Packages.props ./Directory.Packages.props
COPY global.json ./global.json

# Copy all project files (csproj) and restore. Doing targeted copies prevents ambiguous restore context.
# Note: Keep directory structure consistent with repo layout.
COPY src/ApplicationCore/ApplicationCore.csproj src/ApplicationCore/ApplicationCore.csproj
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/Infrastructure.csproj
COPY src/BlazorShared/BlazorShared.csproj src/BlazorShared/BlazorShared.csproj
COPY src/PublicApi/PublicApi.csproj src/PublicApi/PublicApi.csproj
COPY src/BlazorAdmin/BlazorAdmin.csproj src/BlazorAdmin/BlazorAdmin.csproj
COPY src/Web/Web.csproj src/Web/Web.csproj
COPY tests/UnitTests/UnitTests.csproj tests/UnitTests/UnitTests.csproj
COPY tests/FunctionalTests/FunctionalTests.csproj tests/FunctionalTests/FunctionalTests.csproj
COPY tests/IntegrationTests/IntegrationTests.csproj tests/IntegrationTests/IntegrationTests.csproj
COPY tests/PublicApiIntegrationTests/PublicApiIntegrationTests.csproj tests/PublicApiIntegrationTests/PublicApiIntegrationTests.csproj

# Perform restore explicitly on the intended solution to avoid MSB1011 ambiguity
RUN dotnet restore "./eShopOnWeb.sln"

# Copy the remaining source files
COPY . .

# Build the solution explicitly
RUN dotnet build "./eShopOnWeb.sln" -c Release -o /app/build --no-restore

# Publish the Web app (main entry for the monolith)
RUN dotnet publish "src/Web/Web.csproj" -c Release -o /app/publish --no-restore

# =========================
# RUNTIME IMAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
# ASP.NET default listens on 8080 in container scenarios
ENV ASPNETCORE_URLS=http://+:8080

# Copy published app
COPY --from=build /app/publish .

# PUBLIC_INTERFACE
# Entrypoint: start the Web application for eShopOnWeb
# This explicitly specifies the project output DLL to run.
# Summary: resolves MSBuild ambiguity by pre-building against the specified sln and running Web.
# Description: The image builds using the eShopOnWeb.sln solution to avoid MSB1011 errors when multiple projects/solutions exist.
# Returns: process runs Kestrel on port 8080.
ENTRYPOINT ["dotnet", "Web.dll"]
