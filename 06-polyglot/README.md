# Example 6: Polyglot - Multi-Language Services

Orchestrating non-.NET services (Node.js, Python, Go) with Aspire.

## Overview

Aspire is polyglot-friendly! While the AppHost is .NET, you can orchestrate services in any language:
- **Node.js** - JavaScript/TypeScript APIs
- **Python** - FastAPI, Flask, Django
- **Go** - Gin, Echo, net/http
- **Java** - Spring Boot, Quarkus
- **Rust** - Actix, Rocket

## What's Included

This example demonstrates:
- .NET API service (C#)
- Node.js Express service
- Python FastAPI service
- Go HTTP service
- Cross-language service discovery
- Unified observability

## Running

```bash
aspire run
```

## Architecture

```
┌─────────────┐
│  Blazor Web │ (.NET)
│  Frontend   │
└──────┬──────┘
       │
       ├─────► .NET API Service (C#)
       │
       ├─────► Node.js Service (JavaScript)
       │
       ├─────► Python Service (FastAPI)
       │
       └─────► Go Service (net/http)
```

All services:
- Discovered via Aspire
- Visible in dashboard
- Traced with OpenTelemetry
- Share Redis cache

## AppHost Configuration

### Node.js Service

```csharp
var nodeApp = builder.AddNodeApp("node-api", "../node-service", "server.js")
    .WithHttpEndpoint(port: 3000, name: "http")
    .WithReference(cache)  // Inject Redis connection
    .WaitFor(cache);

// Or use npm script:
var nodeApp = builder.AddNpmApp("node-api", "../node-service", "start")
    .WithReference(cache);
```

### Python Service

```csharp
var pythonApp = builder.AddPythonApp("python-api", "../python-service", "app.py")
    .WithHttpEndpoint(port: 8000)
    .WithReference(cache);

// Or with virtual environment:
var pythonApp = builder.AddExecutable("python-api", "python", "../python-service", 
    ["-m", "uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8000"])
    .WithHttpEndpoint(port: 8000)
    .WithReference(cache);
```

### Go Service

```csharp
var goApp = builder.AddExecutable("go-api", "go", "../go-service", ["run", "main.go"])
    .WithHttpEndpoint(port: 8080)
    .WithReference(cache);

// Or pre-built binary:
var goApp = builder.AddExecutable("go-api", "../go-service/bin/api")
    .WithHttpEndpoint(port: 8080);
```

### Java Service

```csharp
var javaApp = builder.AddExecutable("java-api", "java", "../java-service", 
    ["-jar", "target/app.jar"])
    .WithHttpEndpoint(port: 8090);
```

## Service Discovery for Non-.NET Services

Aspire injects environment variables for service discovery:

### Environment Variables Injected

```bash
# For a reference to "api" service:
services__api__http__0=https://localhost:7001
services__api__http__1=http://localhost:5001

# For Redis "cache":
ConnectionStrings__cache=localhost:6379

# For PostgreSQL "db":
ConnectionStrings__db=Host=localhost;Port=5432;Database=appdb;...
```

### Node.js Service Example

```javascript
// server.js
const express = require('express');
const redis = require('redis');

const app = express();
const PORT = process.env.PORT || 3000;

// Aspire injects Redis connection string
const redisClient = redis.createClient({
    url: `redis://${process.env.ConnectionStrings__cache || 'localhost:6379'}`
});

await redisClient.connect();

app.get('/api/data', async (req, res) => {
    const value = await redisClient.get('mykey');
    res.json({ source: 'Node.js', value });
});

app.listen(PORT, () => {
    console.log(`Node.js service running on port ${PORT}`);
});
```

### Python Service Example

```python
# app.py
from fastapi import FastAPI
from redis import Redis
import os

app = FastAPI()

# Aspire injects Redis connection
redis_conn = os.getenv("ConnectionStrings__cache", "localhost:6379")
redis_client = Redis.from_url(f"redis://{redis_conn}")

@app.get("/api/data")
async def get_data():
    value = redis_client.get("mykey")
    return {"source": "Python", "value": value.decode() if value else None}

# Run with: uvicorn app:app --host 0.0.0.0 --port 8000
```

### Go Service Example

```go
// main.go
package main

import (
    "context"
    "encoding/json"
    "net/http"
    "os"

    "github.com/redis/go-redis/v9"
)

func main() {
    // Aspire injects Redis connection
    redisAddr := os.Getenv("ConnectionStrings__cache")
    if redisAddr == "" {
        redisAddr = "localhost:6379"
    }

    rdb := redis.NewClient(&redis.Options{
        Addr: redisAddr,
    })

    http.HandleFunc("/api/data", func(w http.ResponseWriter, r *http.Request) {
        ctx := context.Background()
        value, _ := rdb.Get(ctx, "mykey").Result()
        
        json.NewEncoder(w).Encode(map[string]string{
            "source": "Go",
            "value":  value,
        })
    })

    port := os.Getenv("PORT")
    if port == "" {
        port = "8080"
    }

    http.ListenAndServe(":"+port, nil)
}
```

## OpenTelemetry Integration

For observability, non-.NET services need OpenTelemetry SDKs:

### Node.js Tracing

```javascript
// Install: npm install @opentelemetry/sdk-node @opentelemetry/auto-instrumentations-node

const { NodeSDK } = require('@opentelemetry/sdk-node');
const { getNodeAutoInstrumentations } = require('@opentelemetry/auto-instrumentations-node');
const { OTLPTraceExporter } = require('@opentelemetry/exporter-trace-otlp-http');

const sdk = new NodeSDK({
    traceExporter: new OTLPTraceExporter({
        url: process.env.OTEL_EXPORTER_OTLP_ENDPOINT || 'http://localhost:4318/v1/traces',
    }),
    instrumentations: [getNodeAutoInstrumentations()],
});

sdk.start();
```

### Python Tracing

```python
# Install: pip install opentelemetry-distro opentelemetry-exporter-otlp

from opentelemetry import trace
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor
from opentelemetry.exporter.otlp.proto.http.trace_exporter import OTLPSpanExporter

provider = TracerProvider()
processor = BatchSpanProcessor(OTLPSpanExporter(
    endpoint=os.getenv("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318/v1/traces")
))
provider.add_span_processor(processor)
trace.set_tracer_provider(provider)
```

## Dashboard Integration

All services appear in the Aspire dashboard:
- View logs from Node.js, Python, Go
- Trace requests across .NET and non-.NET
- Monitor health of all services
- Restart/stop any service

## Health Checks

Add health endpoints for better integration:

```javascript
// Node.js
app.get('/health', (req, res) => {
    res.json({ status: 'healthy' });
});
```

```python
# Python
@app.get("/health")
def health():
    return {"status": "healthy"}
```

```go
// Go
http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
    w.Header().Set("Content-Type", "application/json")
    json.NewEncoder(w).Encode(map[string]string{"status": "healthy"})
})
```

## Benefits

1. **Unified Dashboard** - All services in one place
2. **Service Discovery** - No manual endpoint configuration
3. **Distributed Tracing** - Cross-language request tracking
4. **Local Development** - Easy multi-service debugging
5. **Cloud-Ready** - Same patterns work in production

## Limitations

- Non-.NET services don't get ServiceDefaults automatically
- Must manually add OpenTelemetry SDKs
- Health checks are optional but recommended

## CLI Commands

```bash
# View all resources (including non-.NET)
aspire describe

# Restart Node.js service
aspire resource node-api restart

# View Python service logs
aspire resource python-api logs
```

## Next Steps

- Add OpenTelemetry to your non-.NET services
- Implement health check endpoints
- Explore cross-language tracing in dashboard
- Deploy polyglot app to Kubernetes with Aspire manifests
