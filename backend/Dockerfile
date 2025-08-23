# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ProcurementPlanner.sln ./

# Copy project files (excluding tests)
COPY ProcurementPlanner.API/ProcurementPlanner.API.csproj ./ProcurementPlanner.API/
COPY ProcurementPlanner.Core/ProcurementPlanner.Core.csproj ./ProcurementPlanner.Core/
COPY ProcurementPlanner.Infrastructure/ProcurementPlanner.Infrastructure.csproj ./ProcurementPlanner.Infrastructure/

# Restore dependencies for main projects only
RUN dotnet restore ProcurementPlanner.API/ProcurementPlanner.API.csproj

# Copy the rest of the source code (excluding tests)
COPY ProcurementPlanner.API/ ./ProcurementPlanner.API/
COPY ProcurementPlanner.Core/ ./ProcurementPlanner.Core/
COPY ProcurementPlanner.Infrastructure/ ./ProcurementPlanner.Infrastructure/

# Build the application (only main projects)
RUN dotnet build ProcurementPlanner.API/ProcurementPlanner.API.csproj -c Release --no-restore

# Publish the application
RUN dotnet publish ProcurementPlanner.API/ProcurementPlanner.API.csproj -c Release -o /app/publish --no-restore

# Use the official .NET 9 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 5000

# Set the entry point
ENTRYPOINT ["dotnet", "ProcurementPlanner.API.dll"]