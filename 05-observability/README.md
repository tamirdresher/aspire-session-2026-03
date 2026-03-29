# Example 5: Observability

Exploring Aspire's built-in observability dashboard with distributed tracing, metrics, and logs.

## Overview

Aspire provides comprehensive observability out-of-the-box:
- **Distributed Tracing** - Track requests across services
- **Metrics** - Performance counters and custom metrics
- **Structured Logs** - Query logs with filters
- **OpenTelemetry** - Industry-standard telemetry

## What's Included

This example demonstrates:
- Automatic tracing of HTTP calls
- Custom spans and activities
- Metrics collection
- Log correlation
- Dashboard exploration

## Running

```bash
aspire run
```

The dashboard opens at `https://localhost:17000`.

## Dashboard Features

### 1. Resources View

Shows all running services and containers:
- Status (Running, Stopped, Error)
- Endpoints (HTTP, gRPC, etc.)
- Environment variables
- Logs access

### 2. Console Logs

Real-time stdout/stderr from each resource:
- Filtered by resource
- Search within logs
- Timestamps

### 3. Structured Logs

Query logs with powerful filters:
- Filter by log level (Info, Warning, Error)
- Filter by category (namespace)
- Filter by trace ID (see all logs for a request)
- Full-text search

**Example Query:**
```
TraceId = "abc123" AND Level >= "Warning"
```

### 4. Traces

Distributed tracing across services:
- Timeline view of request flow
- Service-to-service calls
- Duration of each operation
- Parent-child span relationships

**Example Trace:**
```
WebApp → API Service → Database
  100ms     50ms         45ms
```

### 5. Metrics

Performance metrics and counters:
- HTTP request rate
- Request duration (p50, p90, p99)
- Error rate
- Custom application metrics
- Container resource usage

## How It Works

### Service Defaults Configuration

Every service calls `builder.AddServiceDefaults()`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Adds OpenTelemetry, health checks, and resilience
builder.AddServiceDefaults();

var app = builder.Build();
app.MapDefaultEndpoints();  // /health, /alive endpoints
app.Run();
```

**What `AddServiceDefaults()` includes:**
- OpenTelemetry tracing (HTTP, gRPC, database)
- OpenTelemetry metrics
- Structured logging with Serilog
- Health checks
- Resilience policies (retry, circuit breaker, timeout)

### Distributed Tracing

HTTP calls are traced automatically:

```csharp
// This HTTP call is automatically traced
var response = await httpClient.GetAsync("http://api/weatherforecast");
```

### Custom Spans

Add custom tracing for important operations:

```csharp
using System.Diagnostics;

public class MyService
{
    private static readonly ActivitySource ActivitySource = new("MyApp.MyService");

    public async Task<Result> ProcessAsync()
    {
        // Create a custom span
        using var activity = ActivitySource.StartActivity("ProcessData");
        activity?.SetTag("operation", "process");
        activity?.SetTag("item.count", items.Count);

        // Your logic here
        var result = await DoWorkAsync();

        activity?.SetTag("result.status", result.Status);
        return result;
    }
}

// In Program.cs, register the activity source:
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("MyApp.MyService"));
```

### Custom Metrics

Add application-specific metrics:

```csharp
using System.Diagnostics.Metrics;

public class OrderService
{
    private static readonly Meter Meter = new("MyApp.Orders");
    private readonly Counter<int> _ordersProcessed;

    public OrderService()
    {
        _ordersProcessed = Meter.CreateCounter<int>("orders.processed");
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // Process order
        await SaveOrderAsync(order);

        // Increment counter
        _ordersProcessed.Add(1, new KeyValuePair<string, object?>("status", order.Status));
    }
}

// In Program.cs:
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter("MyApp.Orders"));
```

### Structured Logging

Use structured logging with semantic meaning:

```csharp
// Bad - string interpolation
logger.LogInformation($"Order {orderId} processed by user {userId}");

// Good - structured
logger.LogInformation("Order processed: {OrderId} by user {UserId}", orderId, userId);
```

Now you can query: `UserId = "123"` in the dashboard.

## Exploring the Dashboard

### Trace a Request End-to-End

1. Navigate to "Traces" tab
2. Click on a trace to expand
3. See timeline: Web → API → Database
4. Click individual spans for details (tags, events, errors)
5. Copy trace ID to query logs with same trace ID

### Find Slow Requests

1. Go to "Traces" tab
2. Sort by duration (descending)
3. Identify bottlenecks
4. Drill into slow spans

### Monitor Error Rate

1. Go to "Metrics" tab
2. View `http.server.request.duration` metric
3. Filter by status code >= 400
4. View error rate over time

### Correlate Logs with Traces

1. Find a failed request in "Traces"
2. Copy the trace ID
3. Go to "Structured Logs"
4. Filter: `TraceId = "<paste-id>"`
5. See all logs for that request across services

## OpenTelemetry Integration

Aspire uses OpenTelemetry under the hood:
- OTLP exporter sends data to dashboard
- Can export to external systems (Jaeger, Zipkin, Prometheus, etc.)
- W3C Trace Context for cross-service correlation

### Exporting to External Systems

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://jaeger:4317");
        });
    });
```

## Best Practices

1. **Use structured logging** - Avoid string interpolation
2. **Add semantic tags** - Use meaningful tag names
3. **Don't over-instrument** - Focus on business operations
4. **Log levels matter** - Use Info, Warning, Error appropriately
5. **Correlation IDs** - Trace IDs are automatic, use them!

## CLI Commands

```bash
# View running resources
aspire describe

# Follow resource state
aspire describe --follow

# View resource logs
aspire resource api logs

# Export telemetry
aspire export --output telemetry.zip
```

## Key Files to Explore

- `ServiceDefaults/Extensions.cs` - OpenTelemetry configuration
- Dashboard at `https://localhost:17000` - Explore all telemetry

## Next Steps

- Add custom spans to track important operations
- Create custom metrics for business KPIs
- Export to external observability platform
- Set up alerting on metrics
