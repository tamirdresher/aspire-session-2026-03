# Example 2: TypeScript AppHost ⭐ NEW in Aspire 13.2

This example demonstrates writing your Aspire AppHost in TypeScript instead of C# - same powerful orchestration, different syntax!

## What's New

Aspire 13.2 introduces **TypeScript AppHost support (preview)**, allowing you to define your application architecture using idiomatic TypeScript while leveraging Aspire's orchestration capabilities.

## Project Structure

```
02-typescript-apphost/
├── apphost.ts              # TypeScript orchestration entry point
├── aspire.config.json      # Aspire configuration
├── package.json            # Node.js dependencies
├── tsconfig.json           # TypeScript configuration
├── .modules/               # Auto-generated integration SDKs (created by aspire restore)
│   └── aspire.js          # Core Aspire TypeScript SDK
└── services/
    ├── api/               # .NET API service
    └── frontend/          # Node.js/Express frontend
```

## Key Concepts

### TypeScript AppHost API

Instead of C# fluent API, you use TypeScript:

```typescript
import { createBuilder } from './.modules/aspire.js';

// Create the builder
const builder = await createBuilder();

// Add a Redis cache
const cache = await builder.addRedis("cache");

// Add .NET API service with cache reference
const api = await builder.addProject("api", "../services/api/api.csproj")
    .withReference(cache)
    .waitFor(cache);

// Add Node.js frontend
const frontend = await builder.addNodeApp("frontend", "../services/frontend", "app.js")
    .withReference(api)
    .withExternalHttpEndpoints();

// Build and run
await builder.build().run();
```

### How It Works

1. Your TypeScript defines the resource graph
2. The Aspire CLI transpiles and runs it as a guest process
3. Communication happens via JSON-RPC with the .NET orchestration host
4. The host handles health checks, dashboard, and service management

### Integration SDK Generation

When you run `aspire add <integration>`, the CLI:
1. Downloads the integration NuGet package
2. Inspects the .NET assembly
3. Generates TypeScript SDK into `.modules/`
4. Provides type-safe APIs for configuration

Run `aspire restore` to regenerate after changes.

## Setup

### Prerequisites
- Node.js 18+ (for TypeScript runtime)
- .NET 9 SDK (for orchestration host and .NET services)
- Aspire CLI 13.2+

### Install Dependencies

```bash
npm install
```

### Initialize Aspire (creates .modules/)

```bash
aspire restore
```

This generates:
- `.modules/aspire.js` - Core TypeScript SDK
- Integration SDKs for Redis, PostgreSQL, etc.

## Running the Example

### Using Aspire CLI

```bash
aspire run
```

The CLI detects `apphost.ts` via `aspire.config.json` and:
1. Compiles TypeScript (if needed)
2. Starts the orchestration host
3. Executes your TypeScript as a guest process
4. Opens the dashboard

### Debugging

```bash
# Run with verbose logging
aspire run --log-level Debug

# Run in detached mode
aspire run --detach
```

## Code Walkthrough

### apphost.ts

```typescript
import { createBuilder } from './.modules/aspire.js';

async function main() {
    const builder = await createBuilder();

    // Container resources
    const redis = await builder.addRedis("cache");
    const postgres = await builder.addPostgres("db");

    // .NET service
    const api = await builder.addProject("api", "../services/api/api.csproj")
        .withReference(redis)
        .withReference(postgres)
        .withEnvironmentVariable("GREETING", "Hello from TypeScript AppHost!");

    // Node.js service
    const web = await builder.addNodeApp("web", "../services/frontend", "server.js")
        .withReference(api)
        .withExternalHttpEndpoints()
        .withEnvironmentVariable("API_URL", api); // Service discovery!

    await builder.build().run();
}

main().catch(console.error);
```

### aspire.config.json

```json
{
  "appHost": {
    "path": "apphost.ts",
    "language": "typescript/nodejs"
  },
  "sdk": {
    "version": "13.2.0"
  },
  "channel": "stable"
}
```

This tells Aspire:
- AppHost is TypeScript (not C#)
- Entry point is `apphost.ts`
- Use Node.js runtime

## TypeScript vs C# Comparison

| Feature | C# AppHost | TypeScript AppHost |
|---------|-----------|-------------------|
| Language | C# | TypeScript |
| API Style | Fluent (method chaining) | Async/await + fluent |
| Type Safety | ✅ Compile-time | ✅ Compile-time |
| Integration SDKs | NuGet packages | Auto-generated `.modules/` |
| Service Discovery | ✅ | ✅ |
| Dashboard | ✅ | ✅ |
| Telemetry | ✅ | ✅ |
| Resource Commands | ✅ | ✅ |

## CLI Commands

```bash
# View generated modules
ls .modules/

# Restore/regenerate integration SDKs
aspire restore

# Add an integration (generates TS SDK)
aspire add redis

# View running resources
aspire describe

# Resource lifecycle
aspire resource api restart
aspire resource web logs
```

## Benefits of TypeScript AppHost

1. **JavaScript/Node.js Developers** - Use familiar syntax
2. **Polyglot Teams** - Less friction for non-.NET developers
3. **Scripting** - Easier to integrate with JS build tooling
4. **Same Power** - Full Aspire features (service discovery, telemetry, etc.)

## Limitations (Preview)

- Some advanced C# AppHost features may not have TS equivalents yet
- Requires Node.js in addition to .NET
- Integration SDK generation requires .NET SDK installed

## Next Steps

- Explore `.modules/aspire.js` to see the generated API
- Add more integrations with `aspire add`
- Compare to Example 1 (C# AppHost) for syntax differences
- Try mixing .NET and Node.js services

## Reference

- [Aspire 13.2 TypeScript AppHost Docs](https://aspire.dev/whats-new/aspire-13-2/#typescript-apphost-support-preview)
- [TypeScript API Reference](https://aspire.dev/reference/typescript-api/)
