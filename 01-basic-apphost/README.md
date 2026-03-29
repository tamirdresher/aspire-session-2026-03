# Example 1: Basic AppHost

This example demonstrates the fundamentals of .NET Aspire's orchestration model with a simple microservices architecture.

## What's Included

- **BasicAppHost.AppHost** - The orchestration project that defines your app's architecture
- **BasicAppHost.ApiService** - ASP.NET Core minimal API backend
- **BasicAppHost.Web** - Blazor frontend application
- **BasicAppHost.ServiceDefaults** - Shared service configuration (telemetry, health checks)

## Key Concepts

### AppHost Orchestration
The AppHost project uses `IDistributedApplicationBuilder` to define your application's structure. Open `Program.cs` to see:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a cache resource (Redis container)
var cache = builder.AddRedis("cache");

// Add the API service with cache reference
var apiService = builder.AddProject<Projects.BasicAppHost_ApiService>("apiservice")
    .WithReference(cache);

// Add the frontend with API reference
builder.AddProject<Projects.BasicAppHost_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(apiService);

builder.Build().Run();
```

**What's happening:**
- `AddRedis()` - Adds a Redis container for caching
- `AddProject<T>()` - References .NET projects as orchestrated resources
- `WithReference()` - Creates service-to-service dependency (automatic service discovery)
- `WithExternalHttpEndpoints()` - Exposes HTTP endpoints externally

### Service Discovery
When you add a reference with `WithReference(apiService)`, Aspire automatically:
1. Injects configuration with the service's endpoint URL
2. Configures HTTP clients to use the correct address
3. Handles health checks and readiness

In the Web project, you can consume the API like this:

```csharp
// Service discovery makes this "just work"
var forecasts = await httpClient.GetFromJsonAsync<WeatherForecast[]>("/weatherforecast");
```

### Service Defaults
The `ServiceDefaults` project adds common observability features to all services:
- OpenTelemetry tracing
- Structured logging
- Health checks
- Resilience (retry, circuit breaker)

Every service calls `builder.AddServiceDefaults()` in its `Program.cs`.

## Running the Example

### Option 1: Using Aspire CLI
```bash
aspire run
```

The dashboard opens at `https://localhost:17000` automatically.

### Option 2: Using Visual Studio
1. Open `BasicAppHost.sln`
2. Set `BasicAppHost.AppHost` as startup project
3. Press F5

### Option 3: Using dotnet run
```bash
dotnet run --project BasicAppHost.AppHost
```

## Exploring the Dashboard

Once running, the Aspire dashboard shows:

1. **Resources** - All running services and containers
2. **Console Logs** - Stdout/stderr from each resource
3. **Structured Logs** - Query logs with filters
4. **Traces** - Distributed traces across services
5. **Metrics** - Performance counters and custom metrics

## Testing the App

1. Open the Web frontend (check dashboard for URL, usually `https://localhost:7xxx`)
2. Navigate to "Weather" page
3. Data flows: Web → API → Redis cache

## Architecture Diagram

```
┌─────────────┐
│   Web       │ (Blazor Frontend)
│  Frontend   │
└──────┬──────┘
       │ HTTP
       ▼
┌─────────────┐
│   API       │ (Minimal API)
│  Service    │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│   Redis     │ (Cache Container)
│   Cache     │
└─────────────┘
```

## Key Files to Explore

- `BasicAppHost.AppHost/Program.cs` - Application architecture definition
- `BasicAppHost.ApiService/Program.cs` - API endpoints and service defaults
- `BasicAppHost.Web/Program.cs` - Frontend setup with HTTP client
- `BasicAppHost.ServiceDefaults/Extensions.cs` - Telemetry configuration

## Next Steps

- Modify the AppHost to add more services
- Add health check endpoints in the API
- Explore distributed tracing in the dashboard
- Try stopping/restarting resources from the dashboard

## CLI Commands to Try

```bash
# View running resources
aspire describe

# Follow resource state changes
aspire describe --follow

# Restart a specific service
aspire resource apiservice restart

# View logs for a resource
aspire resource apiservice logs

# Stop the apphost
aspire stop
```
