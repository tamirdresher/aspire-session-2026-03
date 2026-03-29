# .NET Aspire 13.2 Workshop Examples

This repository contains examples demonstrating key features of .NET Aspire 13.2, prepared for the March 2026 lecture series.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- [Aspire CLI](https://aspire.dev) 13.2 or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop) or Podman
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) with C# Dev Kit

## Setup

Install the Aspire CLI:

```bash
# On Windows (PowerShell)
irm https://aspire.dev/install.ps1 | iex

# On Linux/macOS
curl -sSL https://aspire.dev/install.sh | bash
```

Verify installation:

```bash
aspire --version
dotnet --version
```

## Examples Overview

### 1. [Basic AppHost](./01-basic-apphost/)
Introduction to Aspire's orchestration model with a simple microservices architecture.

- AppHost project with service discovery
- API service + Frontend
- Redis cache integration
- Running with `aspire run`

### 2. [TypeScript AppHost](./02-typescript-apphost/) ⭐ NEW in 13.2
Write your AppHost in TypeScript instead of C# - same powerful orchestration, different syntax.

- TypeScript-based orchestration
- Integration SDKs auto-generated
- Service references and dependencies
- Demonstrates new `aspire init --language typescript`

### 3. [Service Discovery](./03-service-discovery/)
Deep dive into how services find and communicate with each other in Aspire.

- Named service references
- HTTP endpoint resolution
- Configuration-based discovery
- Health checks and readiness

### 4. [Integrations](./04-integrations/)
Working with Aspire's pre-built integration packages for common backing services.

- Redis for caching
- PostgreSQL for data persistence
- RabbitMQ for messaging
- Using `aspire add` to install integrations

### 5. [Observability](./05-observability/)
Exploring Aspire's built-in observability dashboard.

- Distributed tracing
- Metrics and logs
- Structured logging
- OpenTelemetry integration
- Dashboard features

### 6. [Polyglot](./06-polyglot/)
Orchestrating non-.NET services (Node.js, Python, etc.) with Aspire.

- Node.js Express API
- Python FastAPI service
- Service-to-service communication
- Cross-language observability

## Running Examples

Each example has its own README with specific instructions. General pattern:

```bash
cd <example-directory>
aspire run
```

The Aspire dashboard will open automatically at `https://localhost:17000` (default).

## Key Aspire 13.2 Features Demonstrated

- ✅ **TypeScript AppHost** - Write orchestration in TypeScript
- ✅ **CLI Enhancements** - `aspire start`, `aspire ps`, `aspire describe`
- ✅ **Isolated Mode** - Run multiple instances with `--isolated`
- ✅ **Resource Commands** - `aspire resource <name> restart`
- ✅ **Environment Diagnostics** - `aspire doctor`
- ✅ **Detached Mode** - Background execution with `aspire run --detach`

## Reference Materials

- [Official Aspire Documentation](https://aspire.dev)
- [Aspire 13.2 Release Notes](https://aspire.dev/whats-new/aspire-13-2/)
- [Aspire Workshop by Tamir Dresher](https://github.com/tamirdresher/aspire-workshop)
- [Discord Community](https://aka.ms/aspire-discord)

## Troubleshooting

If you encounter issues:

1. Run diagnostics:
   ```bash
   aspire doctor
   ```

2. Check Docker/Podman:
   ```bash
   docker ps
   ```

3. Verify certificates:
   ```bash
   aspire certs trust
   ```

4. Check running apphosts:
   ```bash
   aspire ps
   ```

## Contributing

Questions or improvements? Open an issue or PR!

## License

MIT License - see LICENSE file for details.
