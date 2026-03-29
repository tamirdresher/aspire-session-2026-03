# Example 4: Integrations

Working with Aspire's pre-built integration packages for common backing services.

## Overview

Aspire provides pre-built integrations for popular services:
- **Redis** - Caching and pub/sub
- **PostgreSQL** - Relational database
- **RabbitMQ** - Message queue
- **SQL Server** - Enterprise database
- **MongoDB** - Document database
- **Azure services** - Cosmos DB, Storage, Service Bus, etc.

## What's Included

This example demonstrates:
- Adding integrations with `aspire add`
- Container-based vs. connection string-based resources
- Client configuration and dependency injection
- Data volumes for persistence

## Running

```bash
aspire run
```

## Adding Integrations

### Using Aspire CLI (Recommended)

```bash
# Add Redis integration
cd Integrations.AppHost
aspire add redis

# Add PostgreSQL
aspire add postgres

# Add RabbitMQ
aspire add rabbitmq
```

The CLI automatically:
1. Adds NuGet package to AppHost
2. Adds client package to consuming services
3. Updates references

### Manual Installation

```bash
# In AppHost project
dotnet add package Aspire.Hosting.Redis

# In consuming service
dotnet add package Aspire.StackExchange.Redis
```

## AppHost Configuration

### Redis Integration

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis with data persistence
var redis = builder.AddRedis("cache")
    .WithDataVolume()                    // Persist data
    .WithRedisCommander();               // Add web UI

// Add API with Redis reference
var api = builder.AddProject<Projects.ApiService>("api")
    .WithReference(redis);  // Injects ConnectionStrings__cache
```

### PostgreSQL Integration

```csharp
// Add PostgreSQL server
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()           // Persist database
    .WithPgAdmin();             // Add pgAdmin web UI

// Add specific database
var db = postgres.AddDatabase("appdb");

var api = builder.AddProject<Projects.ApiService>("api")
    .WithReference(db);  // Injects ConnectionStrings__appdb
```

### RabbitMQ Integration

```csharp
var messaging = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();  // Enable management UI at http://localhost:15672

var worker = builder.AddProject<Projects.Worker>("worker")
    .WithReference(messaging);
```

## Client Usage

### Redis Client (StackExchange.Redis)

```csharp
// In Program.cs
builder.AddRedisClient("cache");  // From ServiceDefaults

// In your service
public class CacheService(IConnectionMultiplexer redis)
{
    public async Task<string?> GetAsync(string key)
    {
        var db = redis.GetDatabase();
        return await db.StringGetAsync(key);
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        var db = redis.GetDatabase();
        await db.StringSetAsync(key, value, expiry);
    }
}
```

### PostgreSQL Client (Npgsql + EF Core)

```csharp
// In Program.cs
builder.AddNpgsqlDbContext<AppDbContext>("appdb");

// Entity Framework DbContext
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options) { }

    public DbSet<Product> Products { get; set; }
}

// Usage
public class ProductService(AppDbContext db)
{
    public async Task<List<Product>> GetProductsAsync()
    {
        return await db.Products.ToListAsync();
    }
}
```

### RabbitMQ Client

```csharp
// In Program.cs
builder.AddRabbitMQClient("messaging");

// Usage
public class MessagePublisher(IConnection connection)
{
    public void PublishMessage(string message)
    {
        using var channel = connection.CreateModel();
        channel.QueueDeclare("myqueue", durable: true, exclusive: false);
        
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish("", "myqueue", null, body);
    }
}
```

## Integration Features

### Data Volumes

Persist data across container restarts:

```csharp
builder.AddRedis("cache")
    .WithDataVolume();  // Maps to Docker volume

builder.AddPostgres("db")
    .WithDataVolume();  // Database persists
```

### Management UIs

Many integrations include admin UIs:

```csharp
builder.AddRedis("cache")
    .WithRedisCommander();  // Redis web UI

builder.AddPostgres("db")
    .WithPgAdmin();  // PostgreSQL admin

builder.AddRabbitMQ("mq")
    .WithManagementPlugin();  // RabbitMQ management at :15672
```

### Connection Strings

Aspire injects connection strings automatically:

```csharp
// AppHost defines:
var redis = builder.AddRedis("cache");

// In consuming service:
var conn = configuration.GetConnectionString("cache");
// Result: "localhost:6379"
```

## Health Checks

Integrations include health checks by default:

```csharp
builder.Services.AddHealthChecks()
    .AddRedis(...)      // Automatically added
    .AddNpgsql(...);    // Automatically added
```

View health status in the dashboard.

## Available Integrations

### Databases
- PostgreSQL (`aspire add postgres`)
- SQL Server (`aspire add sqlserver`)
- MySQL (`aspire add mysql`)
- MongoDB (`aspire add mongodb`)

### Caching
- Redis (`aspire add redis`)
- Valkey (`aspire add valkey`)

### Messaging
- RabbitMQ (`aspire add rabbitmq`)
- Kafka (`aspire add kafka`)
- NATS (`aspire add nats`)

### Azure
- Cosmos DB (`aspire add azure-cosmos-db`)
- Storage (`aspire add azure-storage`)
- Service Bus (`aspire add azure-service-bus`)
- Key Vault (`aspire add azure-key-vault`)

### Others
- Elasticsearch (`aspire add elasticsearch`)
- Seq (`aspire add seq`)
- Oracle (`aspire add oracle`)

Full list: https://aspire.dev/integrations/

## CLI Commands

```bash
# List available integrations
aspire add --help

# Add integration
aspire add redis

# View integration documentation
aspire docs search redis
aspire docs get redis-integration
```

## Next Steps

- See Example 5 (Observability) for tracing integration calls
- Explore Example 6 (Polyglot) for non-.NET services with integrations
