using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// AI Agent Orchestrator
// This demonstrates an AI agent that orchestrates and monitors Aspire services

var builder = Host.CreateApplicationBuilder(args);

// Configure HTTP client for service discovery
builder.Services.AddHttpClient();

// Add service discovery (Aspire SDK integration)
builder.Services.AddServiceDiscovery();

// Add the agent orchestrator service
builder.Services.AddHostedService<AgentOrchestrator>();

var host = builder.Build();
await host.RunAsync();

/// <summary>
/// AI Agent Orchestrator - monitors and manages Aspire services
/// </summary>
public class AgentOrchestrator : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AgentOrchestrator(
        IHttpClientFactory httpClientFactory,
        ILogger<AgentOrchestrator> logger,
        IServiceProvider serviceProvider)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🤖 AI Agent Orchestrator starting...");
        
        await Task.Delay(2000, stoppingToken); // Wait for services to start

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await OrchestrationCycle();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestration cycle failed");
            }
        }
    }

    private async Task OrchestrationCycle()
    {
        _logger.LogInformation("=== AI Agent Orchestration Cycle ===");

        // Step 1: Discover services via Aspire service discovery
        await DiscoverServices();

        // Step 2: Check health of services
        await CheckServiceHealth();

        // Step 3: Monitor metrics
        await MonitorMetrics();

        // Step 4: Make orchestration decisions
        await MakeDecisions();

        _logger.LogInformation("=== Cycle Complete ===\n");
    }

    private async Task DiscoverServices()
    {
        _logger.LogInformation("🔍 Discovering services via Aspire service discovery...");

        // Service discovery via environment variables (Aspire pattern)
        var apiUrl = Environment.GetEnvironmentVariable("services__apiservice__http__0");
        var webUrl = Environment.GetEnvironmentVariable("services__webfrontend__http__0");
        var cacheUrl = Environment.GetEnvironmentVariable("ConnectionStrings__cache");

        if (!string.IsNullOrEmpty(apiUrl))
            _logger.LogInformation("  ✓ Found API Service: {Url}", apiUrl);
        
        if (!string.IsNullOrEmpty(webUrl))
            _logger.LogInformation("  ✓ Found Web Frontend: {Url}", webUrl);
        
        if (!string.IsNullOrEmpty(cacheUrl))
            _logger.LogInformation("  ✓ Found Cache: {Url}", cacheUrl);

        // Alternative: Query Aspire Dashboard API for resources
        // This would require dashboard API access (typically internal)
    }

    private async Task CheckServiceHealth()
    {
        _logger.LogInformation("💊 Checking service health...");

        var services = new[]
        {
            ("API Service", Environment.GetEnvironmentVariable("services__apiservice__http__0")),
            ("Web Frontend", Environment.GetEnvironmentVariable("services__webfrontend__http__0"))
        };

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);

        foreach (var (name, baseUrl) in services)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning("  ⚠️  {Service}: Not discovered", name);
                continue;
            }

            try
            {
                var healthUrl = $"{baseUrl}/health";
                var response = await httpClient.GetAsync(healthUrl);

                if (response.IsSuccessStatusCode)
                {
                    var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
                    _logger.LogInformation("  ✓ {Service}: {Status}", name, health?.Status ?? "Healthy");
                }
                else
                {
                    _logger.LogWarning("  ⚠️  {Service}: Unhealthy (HTTP {StatusCode})", name, response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("  ❌ {Service}: {Error}", name, ex.Message);
            }
        }
    }

    private async Task MonitorMetrics()
    {
        _logger.LogInformation("📊 Monitoring service metrics...");

        // In a real scenario, you would:
        // 1. Query Aspire dashboard API for metrics
        // 2. Query OpenTelemetry collector
        // 3. Use Aspire SDK to access telemetry

        var apiUrl = Environment.GetEnvironmentVariable("services__apiservice__http__0");
        if (!string.IsNullOrEmpty(apiUrl))
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync($"{apiUrl}/weatherforecast");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("  ✓ API Service: Responding normally");
                    _logger.LogInformation("    Response Time: {Ms}ms", 
                        response.Headers.TryGetValues("X-Response-Time", out var times) 
                            ? times.First() 
                            : "N/A");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("  ⚠️  API Service: Failed to get metrics - {Error}", ex.Message);
            }
        }
    }

    private Task MakeDecisions()
    {
        _logger.LogInformation("🧠 Making orchestration decisions...");

        // AI Agent Decision Logic:
        // - If services are healthy: Continue monitoring
        // - If services are degraded: Could trigger scaling, alerts, etc.
        // - If services are down: Could restart, failover, etc.

        // For this demo, we just log the decision
        _logger.LogInformation("  ℹ️  Decision: All services nominal, continuing monitoring");

        // In a real AI agent, you might:
        // - Use LLM to analyze logs and make decisions
        // - Trigger automated remediation
        // - Adjust resource allocation
        // - Send notifications via Teams, Slack, etc.

        return Task.CompletedTask;
    }
}

public record HealthResponse(string Status);
