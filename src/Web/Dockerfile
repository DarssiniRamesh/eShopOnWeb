# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and props to enable restore at solution level
COPY eShopOnWeb.sln ./
COPY Directory.Packages.props ./
COPY global.json ./

# Copy project files explicitly to leverage Docker layer caching
# Application projects
COPY src/ApplicationCore/ApplicationCore.csproj src/ApplicationCore/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/PublicApi/PublicApi.csproj src/PublicApi/
COPY src/BlazorShared/BlazorShared.csproj src/BlazorShared/
COPY src/BlazorAdmin/BlazorAdmin.csproj src/BlazorAdmin/
COPY src/Web/Web.csproj src/Web/

# Test projects (not required for runtime but included to satisfy solution restore references if any)
COPY tests/UnitTests/UnitTests.csproj tests/UnitTests/
COPY tests/FunctionalTests/FunctionalTests.csproj tests/FunctionalTests/
COPY tests/IntegrationTests/IntegrationTests.csproj tests/IntegrationTests/
COPY tests/PublicApiIntegrationTests/PublicApiIntegrationTests.csproj tests/PublicApiIntegrationTests/

# Restore solution to ensure all referenced projects are resolved.
RUN dotnet restore eShopOnWeb.sln

# Copy remaining source
COPY . .

# Build and publish the Web project explicitly to avoid MSBuild ambiguity
WORKDIR /src/src/Web
RUN dotnet build Web.csproj -c Release -o /app/build
RUN dotnet publish Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Non-root user for better security where supported
# Note: If running in environments that require root, comment these lines.
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .
# Expose default ASP.NET Core port
EXPOSE 8080
# ASPNETCORE_URLS to listen on container port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Start the Web project
# PUBLIC_INTERFACE
# Purpose: Entrypoint for the eShopOnWeb Web application inside the container.
# Accepts: ASPNETCORE_* environment variables to configure hosting/urls.
# Returns: Runs the ASP.NET Core Kestrel server serving the MVC web application.
# Note: PublicApi and BlazorAdmin should have their own Dockerfiles under src/PublicApi and src/BlazorAdmin respectively.
ENTRYPOINT ["dotnet", "Web.dll"]
