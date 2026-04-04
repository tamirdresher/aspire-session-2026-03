# Example 7: AI Agent Integration with Aspire (Microsoft Agent Framework GA)

This example shows how to build an **AI-powered agent** that orchestrates and monitors a
distributed Aspire application using the **Microsoft Agent Framework 1.0.0 (GA)**.

The orchestrator uses LLM tool-calling to autonomously discover services, check health,
collect metrics, and generate natural-language SRE reports — all running inside an Aspire
BackgroundService.

---

## What Changed from the Preview?

This example was updated from a manual polling loop to the **Microsoft Agent Framework GA
(1.0.0)**, which supersedes AutoGen and Semantic Kernel for agentic workloads:

| Before (preview) | After (GA) |
|---|---|
| Manual `BackgroundService` with HTTP polling | `AIAgent` with autonomous tool-calling |
| Hardcoded decision logic | LLM-driven analysis and recommendations |
| No LLM integration | Azure OpenAI via `AsAIAgent()` extension |
| `OpenAIClient` manual calls | `AIFunctionFactory.Create()` tool registration |

---

## What About Jupyter Notebooks?

> **Short answer: Jupyter notebooks are NOT the recommended approach for Microsoft Agent Framework.**

The old Semantic Kernel learning path used `.ipynb` Jupyter notebooks as interactive
demos.  The Microsoft Agent Framework GA replaces this model with:

1. **Regular C#/Python projects** — structured, testable, production-ready (this example)
2. **DevUI** — the new interactive developer UI (install `devui` package) for step-by-step
   agent debugging, tool introspection, and conversation replay — no Jupyter needed
3. **Sample projects** in the `microsoft/agent-framework` GitHub repo
   (`dotnet/samples/`, `python/samples/`)

If you want the interactive notebook-style experience, use DevUI instead of Jupyter.

---

## Architecture

```
Aspire AppHost
├── apiservice         (ASP.NET Core API + Redis)
├── webfrontend        (Blazor frontend)
├── cache              (Redis)
└── orchestrator       ← AI Agent Orchestrator (this example)
        │
        │  Microsoft Agent Framework GA
        │  ┌─────────────────────────────────┐
        │  │  AIAgent (Azure OpenAI-backed)  │
        │  │  ┌─────────────────────────┐    │
        │  │  │  Tool: DiscoverServices │    │
        │  │  │  Tool: CheckHealth      │    │
        │  │  │  Tool: GetMetrics       │    │
        │  │  └─────────────────────────┘    │
        │  └─────────────────────────────────┘
        │
        └──► Discovers apiservice & webfrontend
             via Aspire service discovery env vars
```

---

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Aspire workload](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)
- An **Azure OpenAI** resource with a deployed model (e.g., `gpt-4o-mini`)
- Run `az login` once to enable `DefaultAzureCredential`

### Configure Azure OpenAI (one-time)

```bash
cd 07-ai-agent-integration/AIAgent.AppHost

dotnet user-secrets set "azure-openai-endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "azure-openai-deployment" "gpt-4o-mini"
```

> **No Azure OpenAI?** The orchestrator falls back to basic (non-LLM) monitoring mode
> automatically. You will still see health checks and metrics in the logs.

### Run

```bash
dotnet run --project AIAgent.AppHost
```

Open the **Aspire Dashboard** link in the console, then click on the **orchestrator**
resource to watch the agent logs in real-time.

---

## How It Works

### 1. Agent Setup (Microsoft Agent Framework GA)

```csharp
// Create an AIAgent backed by Azure OpenAI — single line!
AIAgent agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
    .GetChatClient(deployment)
    .AsAIAgent(
        name: "AspireSREAgent",
        instructions: "You are an SRE agent monitoring an Aspire application...",
        tools:
        [
            AIFunctionFactory.Create(DiscoverServices),
            AIFunctionFactory.Create(CheckServiceHealth),
            AIFunctionFactory.Create(GetServiceMetrics)
        ]);
```

### 2. Tool Registration

Tools are plain C# methods decorated with `[Description]` attributes.  The framework
uses these descriptions to teach the LLM when and how to call each tool.

```csharp
[Description("Discovers all microservices via Aspire service discovery env vars.")]
private Task<string> DiscoverServices() { ... }

[Description("Checks liveness of a named Aspire service via /health endpoint.")]
private async Task<string> CheckServiceHealth(
    [Description("Service name: 'apiservice' or 'webfrontend'")] string serviceName) { ... }
```

### 3. Autonomous Run Loop

```csharp
// The agent autonomously decides which tools to call and in what order.
var result = await agent.RunAsync(
    "Analyze all Aspire services and report on their health.");

_logger.LogInformation("Agent report:\n{Report}", result);
```

Example agent output:
```
All systems operational.
- API Service: Healthy (42ms)
- Web Frontend: Healthy (18ms)
- Redis Cache: Connected
No action required.
```

### 4. Aspire Service Discovery

Aspire injects service endpoints as environment variables — the agent uses these to
find services without hardcoded URLs:

```csharp
var apiUrl = Environment.GetEnvironmentVariable("services__apiservice__http__0");
var webUrl = Environment.GetEnvironmentVariable("services__webfrontend__http__0");
```

---

## Key Packages (csproj)

```xml
<PackageReference Include="Microsoft.Agents.AI.AzureAI"   Version="1.0.0" />
<PackageReference Include="Azure.AI.OpenAI"               Version="2.2.0" />
<PackageReference Include="Azure.Identity"                Version="1.13.2" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="9.6.0" />
```

---

## Further Reading

- [Microsoft Agent Framework GA Announcement](https://github.com/microsoft/agent-framework)
- [Agent Framework Samples](https://github.com/microsoft/agent-framework/tree/main/dotnet/samples)
- [DevUI — interactive agent debugging](https://github.com/microsoft/agent-framework/tree/main/devui)
- [.NET Aspire Service Discovery](https://learn.microsoft.com/dotnet/aspire/service-discovery/overview)
- [DefaultAzureCredential](https://learn.microsoft.com/azure/developer/intro/passwordless-overview)

---

> **This example shows Aspire as the perfect infrastructure layer for AI-driven distributed
> systems: automatic service discovery, unified observability, and built-in health checks —
> all seamlessly consumed by a Microsoft Agent Framework GA agent.**
