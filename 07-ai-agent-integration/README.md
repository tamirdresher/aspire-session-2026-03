# Example 7: AI Agent Integration with Aspire

This example demonstrates how AI agents can orchestrate and monitor distributed systems using Aspire as the infrastructure layer.

## Overview

Modern AI agents (like Squad, GitHub Copilot, autonomous systems) need to:
- Discover and interact with microservices
- Monitor system health and metrics
- Make intelligent orchestration decisions
- Report status and take automated actions

Aspire provides the perfect infrastructure foundation for AI-driven distributed systems.

## What's Included

- **AIAgent.Orchestrator** - C# console app acting as an AI agent
- **AIAgent.ApiService** - Sample .NET API to monitor
- **AIAgent.Web** - Sample Blazor frontend to monitor
- **Redis Cache** - Backing service to monitor

## Architecture

```
┌─────────────────────────────┐
│   AI Agent Orchestrator     │
│   (Autonomous Monitor)      │
└──────────┬──────────────────┘
           │
           ├─► Discovers services via Aspire service discovery
           ├─► Checks health endpoints
           ├─► Monitors metrics & telemetry
           └─► Makes orchestration decisions
                     │
      ┌──────────────┴──────────────┐
      │                             │
      ▼                             ▼
┌──────────┐                 ┌──────────┐
│   API    │◄───────────────►│   Web    │
│ Service  │                 │ Frontend │
└────┬─────┘                 └──────────┘
     │
     ▼
┌──────────┐
│  Redis   │
│  Cache   │
└──────────┘
```

## Running the Example

```bash
aspire run
```

Watch the **orchestrator** logs in the Aspire dashboard to see the AI agent in action:
- 🔍 **Service Discovery** - Agent finds services automatically
- 💊 **Health Checks** - Agent monitors service health
- 📊 **Metrics Collection** - Agent gathers performance data
- 🧠 **Decision Making** - Agent makes orchestration decisions

## How It Works

### 1. Service Discovery

The AI agent uses Aspire's service discovery to find services:

```csharp
// Aspire injects service URLs as environment variables
var apiUrl = Environment.GetEnvironmentVariable("services__apiservice__http__0");
var webUrl = Environment.GetEnvironmentVariable("services__webfrontend__http__0");
var cacheUrl = Environment.GetEnvironmentVariable("ConnectionStrings__cache");
```

**Key Point:** The agent doesn't hardcode endpoints - Aspire provides them dynamically.

### 2. Health Monitoring

The agent periodically checks service health:

```csharp
private async Task CheckServiceHealth()
{
    var services = new[]
    {
        ("API Service", apiUrl),
        ("Web Frontend", webUrl)
    };

    foreach (var (name, baseUrl) in services)
    {
        var healthUrl = $"{baseUrl}/health";
        var response = await httpClient.GetAsync(healthUrl);
        
        if (response.IsSuccessStatusCode)
            _logger.LogInformation("✓ {Service}: Healthy", name);
        else
            _logger.LogWarning("⚠️ {Service}: Unhealthy", name);
    }
}
```

### 3. Metrics Collection

The agent monitors service performance:

```csharp
private async Task MonitorMetrics()
{
    var response = await httpClient.GetAsync($"{apiUrl}/weatherforecast");
    
    if (response.IsSuccessStatusCode)
    {
        _logger.LogInformation("✓ API Service: Responding normally");
        // Could extract response time, error rates, etc.
    }
}
```

In production, the agent could:
- Query Aspire dashboard API for detailed metrics
- Access OpenTelemetry collector
- Analyze distributed traces

### 4. Autonomous Decision Making

The agent makes decisions based on observations:

```csharp
private Task MakeDecisions()
{
    // AI Agent Decision Logic:
    // - If services are healthy: Continue monitoring
    // - If services are degraded: Trigger scaling, alerts
    // - If services are down: Restart, failover

    _logger.LogInformation("🧠 Making orchestration decisions...");
    
    // In a real AI agent:
    // - Use LLM to analyze logs and patterns
    // - Trigger automated remediation
    // - Adjust resource allocation
    // - Send notifications via Teams, Slack, etc.
    
    return Task.CompletedTask;
}
```

## Agent Orchestration Cycle

The agent runs continuously in a loop:

```
1. Discover Services (via Aspire service discovery)
   ↓
2. Check Health (HTTP health endpoints)
   ↓
3. Monitor Metrics (API calls, response times)
   ↓
4. Make Decisions (analyze and act)
   ↓
5. Wait (10 seconds)
   ↓
   [Repeat]
```

## AI Agents + Aspire: Use Cases

### 1. Autonomous Site Reliability Engineering (SRE)
An AI agent monitors production services and:
- Detects anomalies in metrics
- Diagnoses root causes using logs/traces
- Automatically scales resources
- Restarts unhealthy services
- Posts incident reports to Teams

### 2. Smart Load Balancing
An AI agent analyzes traffic patterns and:
- Discovers all service instances
- Monitors response times and error rates
- Adjusts routing rules dynamically
- Predicts load spikes

### 3. Cost Optimization
An AI agent tracks resource usage and:
- Identifies underutilized services
- Recommends right-sizing
- Schedules workloads during off-peak hours
- Generates cost reports

### 4. Security Monitoring
An AI agent watches for threats:
- Monitors authentication failures
- Detects unusual traffic patterns
- Blocks suspicious IPs
- Sends security alerts

### 5. Development Assistants (like Squad!)
An AI development team uses Aspire to:
- Discover running services in the repo
- Monitor build/test pipelines
- Restart failed services
- Report status to developers via Teams

## Extending the Agent

### Add LLM Integration

```csharp
private async Task<string> AnalyzeWithLLM(string logs)
{
    var client = new OpenAIClient(apiKey);
    var prompt = $@"
        You are an SRE agent. Analyze these service logs and recommend actions:
        {logs}
    ";
    
    var response = await client.GetCompletionAsync(prompt);
    return response.Choices[0].Text;
}
```

### Add Teams Notifications

```csharp
private async Task NotifyTeams(string message)
{
    var webhookUrl = Environment.GetEnvironmentVariable("TEAMS_WEBHOOK_URL");
    var payload = new { text = message };
    
    await httpClient.PostAsJsonAsync(webhookUrl, payload);
}
```

### Add Auto-Scaling

```csharp
private async Task ScaleService(string serviceName, int replicas)
{
    // Use Aspire API or Kubernetes API to scale
    _logger.LogInformation("Scaling {Service} to {Replicas} replicas", serviceName, replicas);
    
    // In Kubernetes:
    // await kubernetesClient.ScaleDeployment(serviceName, replicas);
}
```

## Benefits of Aspire for AI Agents

1. **Service Discovery** - Agents find services automatically (no hardcoded URLs)
2. **Unified Observability** - Single dashboard for logs, traces, metrics
3. **Health Checks** - Built-in health endpoints (`/health`, `/alive`)
4. **Resilience** - Retry policies, circuit breakers included
5. **Local-to-Production Parity** - Same patterns work everywhere

## CLI Commands

```bash
# View agent logs in real-time
aspire resource orchestrator logs --follow

# Check agent health
aspire describe orchestrator

# Restart agent
aspire resource orchestrator restart

# See all discovered services
aspire describe
```

## Configuration

The agent uses Aspire service discovery automatically. No configuration needed!

Environment variables are injected by Aspire:
- `services__<name>__http__0` - Service HTTP endpoints
- `ConnectionStrings__<name>` - Connection strings for backing services

## Sample Agent Output

```
🤖 AI Agent Orchestrator starting...

=== AI Agent Orchestration Cycle ===
🔍 Discovering services via Aspire service discovery...
  ✓ Found API Service: https://localhost:7001
  ✓ Found Web Frontend: https://localhost:7002
  ✓ Found Cache: localhost:6379

💊 Checking service health...
  ✓ API Service: Healthy
  ✓ Web Frontend: Healthy

📊 Monitoring service metrics...
  ✓ API Service: Responding normally
    Response Time: 45ms

🧠 Making orchestration decisions...
  ℹ️  Decision: All services nominal, continuing monitoring

=== Cycle Complete ===
```

## Real-World Integration

To integrate with real AI agents (like Squad):

1. **Agent reads Aspire dashboard** - Via dashboard API or CLI
2. **Agent discovers services** - Using service discovery patterns
3. **Agent monitors health** - Via `/health` endpoints
4. **Agent takes actions** - Restart, scale, notify, remediate
5. **Agent reports status** - Via Teams, logs, tickets

## Next Steps

- Add LLM integration for intelligent decision-making
- Connect to monitoring systems (Prometheus, Grafana)
- Implement auto-remediation workflows
- Add Teams/Slack notifications
- Deploy agent to production with real workloads

## Key Files

- `AIAgent.Orchestrator/Program.cs` - Agent implementation
- `AIAgent.AppHost/AppHost.cs` - Aspire orchestration setup

---

**This example shows Aspire as the perfect infrastructure layer for AI-driven distributed systems, enabling autonomous monitoring, intelligent orchestration, and seamless service discovery.**
