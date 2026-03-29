# Example 3: Service Discovery

Deep dive into how services find and communicate with each other in Aspire.

## Overview

Service discovery is Aspire's mechanism for:
- Services finding each other at runtime
- Automatic endpoint resolution
- Configuration injection
- Health-aware routing

## What You'll Learn

- How `WithReference()` enables service discovery
- Configuration-based endpoint resolution
- Health checks and readiness probing
- Testing service-to-service communication

## Running

```bash
aspire run
```

## Key Concepts

### Named References
When you add a reference, Aspire injects configuration:

```csharp
var api = builder.AddProject<Projects.ApiService>("api");
var web = builder.AddProject<Projects.WebApp>("web")
    .WithReference(api);  // ← Service discovery magic
```

In the `web` project, you can resolve the API endpoint:

```csharp
// Configuration automatically contains:
// ConnectionStrings__api = "http://localhost:5001"

var apiUrl = configuration.GetConnectionString("api");
```

### HTTP Client Integration

Aspire integrates with HttpClientFactory:

```csharp
builder.Services.AddHttpClient<IApiClient, ApiClient>(client => 
{
    client.BaseAddress = new Uri(builder.Configuration.GetConnectionString("api")!);
});
```

### Health Checks

Services can wait for dependencies:

```csharp
builder.AddProject<Projects.ApiService>("api")
    .WaitFor(redis)  // Wait for Redis to be healthy
    .WaitForCompletion(); // Wait for migrations/init
```

## Architecture

```
WebApp → (discovers) → ApiService → (discovers) → Redis
```

All endpoint resolution happens automatically at runtime.

## Next Steps

- Explore Example 4 (Integrations) for Redis/PostgreSQL
- See Example 5 (Observability) for tracing discovery requests
