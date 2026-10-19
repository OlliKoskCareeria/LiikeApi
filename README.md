# Liike API

Liike is an ASP.NET Core API for publishing and managing local sport events. Public clients can browse published events; organizer and administrator workflows require JWT authentication.

## Technology

- .NET 8 and ASP.NET Core Web API
- PostgreSQL with Entity Framework Core and Npgsql
- JWT bearer authentication and ASP.NET Core password hashing
- Nominatim (OpenStreetMap) for event-address geocoding
- Layered solution: `Liike` (API), `Liike.Application`, `Liike.Domain`, and `Liike.Infrastructure`

## Run locally

Requirements: .NET 8 SDK and access to a PostgreSQL instance. Configure a connection string that points to the target database, then run the EF Core migrations below to create or update its schema.

Configure the local database connection, JWT signing key, and geocoding User-Agent with .NET User Secrets. Keep credentials out of source control.

Install the EF Core command-line tool if needed, then restore packages, apply migrations, and start the API from the repository root:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet restore .\Liike\Liike.csproj
dotnet ef database update --project .\Liike.Infrastructure\Liike.Infrastructure.csproj --startup-project .\Liike\Liike.csproj
dotnet run --project .\Liike\Liike.csproj --launch-profile https
```

In Development, Swagger UI is available at `https://localhost:7055/swagger` when using the HTTPS launch profile. The repository does not seed or provide a bootstrap flow for an administrator account; one must be provisioned separately before administrator-only operations are available.

For endpoint routes, request/response formats, and frontend authentication details, see [FRONTEND_API_GUIDE.md](FRONTEND_API_GUIDE.md).
