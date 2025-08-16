# Procurement Planner Backend

## Project Structure

This solution follows Clean Architecture principles with the following projects:

### ProcurementPlanner.API
- **Purpose**: Web API layer containing controllers, middleware, and API configuration
- **Dependencies**: Core, Infrastructure
- **Key Features**:
  - RESTful API endpoints
  - JWT Authentication
  - Global exception handling
  - CORS configuration
  - Serilog logging
  - FluentValidation

### ProcurementPlanner.Core
- **Purpose**: Domain layer containing entities, interfaces, and business logic
- **Dependencies**: None (pure domain layer)
- **Key Features**:
  - Domain entities
  - Repository interfaces
  - Business rules and validation

### ProcurementPlanner.Infrastructure
- **Purpose**: Data access layer containing Entity Framework configuration and implementations
- **Dependencies**: Core
- **Key Features**:
  - Entity Framework DbContext
  - Repository implementations
  - Database migrations
  - External service integrations

### ProcurementPlanner.Tests
- **Purpose**: Unit and integration tests
- **Dependencies**: API, Core, Infrastructure
- **Key Features**:
  - xUnit test framework
  - Moq for mocking
  - FluentAssertions for readable assertions
  - In-memory database for testing

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- SQL Server LocalDB (for development)
- Redis (optional, for caching)

### Running the Application

1. **Build the solution:**
   ```bash
   dotnet build
   ```

2. **Run tests:**
   ```bash
   dotnet test
   ```

3. **Start the API:**
   ```bash
   dotnet run --project ProcurementPlanner.API
   ```

4. **Access the API:**
   - Health check: `GET https://localhost:7000/api/health`
   - OpenAPI documentation: `https://localhost:7000/openapi/v1.json`

### Configuration

The application uses the following configuration files:
- `appsettings.json` - Production settings
- `appsettings.Development.json` - Development overrides

Key configuration sections:
- **ConnectionStrings**: Database and Redis connections
- **Jwt**: JWT authentication settings
- **Serilog**: Logging configuration

### Database Setup

The application uses Entity Framework Core with SQL Server. Database migrations will be created in future tasks.

### Logging

The application uses Serilog for structured logging with the following sinks:
- Console (for development)
- File (rolling daily logs in `logs/` directory)

### Authentication

JWT-based authentication is configured but will be implemented in future tasks.

### CORS

CORS is configured to allow requests from React development server (`http://localhost:3000`).

## Implementation Status

### ✅ Completed (Task 1)
- [x] Solution structure with separate projects for API, Core, Infrastructure, and Tests
- [x] Entity Framework Core with SQL Server connection configured
- [x] Dependency injection container setup
- [x] Logging with Serilog (console and file output)
- [x] CORS configuration for React frontend
- [x] JWT authentication infrastructure (ready for implementation)
- [x] Global exception handling middleware
- [x] Health checks endpoints
- [x] AutoMapper configuration
- [x] FluentValidation setup
- [x] Integration testing framework with WebApplicationFactory
- [x] Basic API response models

### 🔄 Next Tasks
- [ ] Authentication and authorization system implementation
- [ ] Domain models and database schema creation
- [ ] Business services and API controllers
- [ ] Redis caching integration
- [ ] Additional middleware and features