// AI Agent Orchestrator — Microsoft Agent Framework GA edition
// Uses the official Microsoft Agent Framework (1.0.0) with Azure OpenAI for LLM-powered
// SRE decisions. Aspire provides service discovery, observability, and health infrastructure.
//
// Required environment variables (injected by Aspire AppHost or user-secrets):
//   AZURE_OPENAI_ENDPOINT           — your Azure OpenAI endpoint URL
//   AZURE_OPENAI_DEPLOYMENT_NAME    — model deployment name (default: gpt-4o-mini)
//
// Authentication: uses DefaultAzureCredential (run `az login` locally)

using System.ComponentModel;
using System.Diagnostics;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddServiceDiscovery();
builder.Services.AddHostedService<AgentOrchestrator>();

var host = builder.Build();
await host.RunAsync();

/// <summary>
/// AI Agent Orchestrator using Microsoft Agent Framework GA.
/// The agent uses tool-calling to discover services, check health, and collect metrics,
/// then produces an LLM-driven analysis and recommended actions.
/// </summary>
public class AgentOrchestrator : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AgentOrchestrator> _logger;
    private HttpClient? _http;
    private AIAgent? _agent;

    public AgentOrchestrator(IHttpClientFactory factory, ILogger<AgentOrchestrator> logger)
    {
        _httpClientFactory = factory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _http = _httpClientFactory.CreateClient();
        _http.Timeout = TimeSpan.FromSeconds(5);

        _agent = BuildAgent();

        _logger.LogInformation("🤖 AI Agent Orchestrator starting (Microsoft Agent Framework {Version})...",
            typeof(AIAgent).Assembly.GetName().Version);

        // Give Aspire services a moment to fully start
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOrchestrationCycle(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Orchestration cycle failed"); }
        }
    }

    // -------------------------------------------------------------------------
    // Agent construction
    // -------------------------------------------------------------------------

    private AIAgent? BuildAgent()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
        var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";

        if (string.IsNullOrEmpty(endpoint))
        {
            _logger.LogWarning(
                "⚠️  AZURE_OPENAI_ENDPOINT not set — running in basic (non-LLM) monitoring mode. " +
                "Set this via Aspire parameters or user-secrets to enable Agent Framework intelligence.");
            return null;
        }

        // Microsoft Agent Framework GA: create an AIAgent backed by Azure OpenAI.
        // The agent is given tools (C# methods) it can call autonomously via tool-calling.
        var agent = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential())
            .GetChatClient(deployment)
            .AsAIAgent(
                name: "AspireSREAgent",
                instructions: """
                    You are an intelligent SRE agent monitoring an Aspire distributed application.
                    You have tools to discover services, check their health, and collect metrics.
                    
                    When asked to analyze the system:
                    1. Call DiscoverServices to see what services are running.
                    2. Call CheckServiceHealth for each discovered service.
                    3. Call GetServiceMetrics for the API service.
                    4. Summarize the system state concisely (3-5 lines).
                    5. If any service is unhealthy or slow (>200ms), suggest a remediation step.
                    
                    Keep your response concise. Start with an emoji status summary line.
                    """,
                tools:
                [
                    AIFunctionFactory.Create(DiscoverServices),
                    AIFunctionFactory.Create(CheckServiceHealth),
                    AIFunctionFactory.Create(GetServiceMetrics)
                ]);

        _logger.LogInformation("✅ Microsoft Agent Framework agent initialized (model: {Model})", deployment);
        return agent;
    }

    // -------------------------------------------------------------------------
    // Orchestration cycle
    // -------------------------------------------------------------------------

    private async Task RunOrchestrationCycle(CancellationToken ct)
    {
        _logger.LogInformation("=== AI Agent Orchestration Cycle ===");

        if (_agent is not null)
        {
            // The agent autonomously calls the registered tools (DiscoverServices,
            // CheckServiceHealth, GetServiceMetrics) and synthesises a health report.
            var result = await _agent.RunAsync(
                "Analyze the current state of all Aspire services and report on their health.",
                cancellationToken: ct);

            _logger.LogInformation("🧠 Agent report:\n{Report}", result);
        }
        else
        {
            // Fallback: run tools directly without LLM analysis
            _logger.LogInformation("Running in basic monitoring mode (no LLM)");
            _logger.LogInformation("{Services}", await DiscoverServices());
            _logger.LogInformation("{Health}", await CheckServiceHealth("apiservice"));
            _logger.LogInformation("{Health}", await CheckServiceHealth("webfrontend"));
            _logger.LogInformation("{Metrics}", await GetServiceMetrics("apiservice"));
        }

        _logger.LogInformation("=== Cycle Complete ===\n");
    }

    // -------------------------------------------------------------------------
    // Agent tools — the Agent Framework discovers these via AIFunctionFactory
    // and exposes them to the LLM for autonomous invocation (tool-calling).
    // -------------------------------------------------------------------------

    [Description("Discovers all microservices registered in the Aspire application via Aspire service discovery environment variables.")]
    private Task<string> DiscoverServices()
    {
        var found = new List<string>();

        var apiUrl = Environment.GetEnvironmentVariable("services__apiservice__http__0");
        var webUrl = Environment.GetEnvironmentVariable("services__webfrontend__http__0");
        var cacheUrl = Environment.GetEnvironmentVariable("ConnectionStrings__cache");

        if (!string.IsNullOrEmpty(apiUrl)) found.Add($"API Service ({apiUrl})");
        if (!string.IsNullOrEmpty(webUrl)) found.Add($"Web Frontend ({webUrl})");
        if (!string.IsNullOrEmpty(cacheUrl)) found.Add($"Redis Cache ({cacheUrl})");

        return Task.FromResult(found.Count > 0
            ? $"Discovered {found.Count} service(s):\n" + string.Join("\n", found.Select(s => $"  * {s}"))
            : "No services discovered. Aspire service discovery environment variables are not set.");
    }

    [Description("Checks the liveness and readiness of a named Aspire service by calling its /health endpoint.")]
    private async Task<string> CheckServiceHealth(
        [Description("Service name to check. Valid values: 'apiservice', 'webfrontend'")] string serviceName)
    {
        var url = serviceName.ToLowerInvariant() switch
        {
            "apiservice"  => Environment.GetEnvironmentVariable("services__apiservice__http__0"),
            "webfrontend" => Environment.GetEnvironmentVariable("services__webfrontend__http__0"),
            _             => null
        };

        if (string.IsNullOrEmpty(url))
            return $"'{serviceName}': not found in Aspire service discovery.";

        try
        {
            var resp = await _http!.GetAsync($"{url}/health");
            return resp.IsSuccessStatusCode
                ? $"'{serviceName}': Healthy (HTTP {(int)resp.StatusCode})"
                : $"'{serviceName}': Unhealthy (HTTP {(int)resp.StatusCode})";
        }
        catch (Exception ex)
        {
            return $"'{serviceName}': Unreachable - {ex.Message}";
        }
    }

    [Description("Collects performance metrics (response time, status) from a named Aspire service.")]
    private async Task<string> GetServiceMetrics(
        [Description("Service name to measure. Valid values: 'apiservice'")] string serviceName)
    {
        var url = serviceName.ToLowerInvariant() switch
        {
            "apiservice" => Environment.GetEnvironmentVariable("services__apiservice__http__0"),
            _            => null
        };

        if (string.IsNullOrEmpty(url))
            return $"No metrics endpoint available for '{serviceName}'.";

        try
        {
            var sw = Stopwatch.StartNew();
            var resp = await _http!.GetAsync($"{url}/weatherforecast");
            sw.Stop();

            var status = resp.IsSuccessStatusCode ? "OK" : $"Error ({(int)resp.StatusCode})";
            var latency = sw.ElapsedMilliseconds;
            var warning = latency > 200 ? " HIGH LATENCY" : "";
            return $"'{serviceName}' metrics: status={status}, responseTime={latency}ms{warning}";
        }
        catch (Exception ex)
        {
            return $"'{serviceName}': Failed to collect metrics - {ex.Message}";
        }
    }
}
