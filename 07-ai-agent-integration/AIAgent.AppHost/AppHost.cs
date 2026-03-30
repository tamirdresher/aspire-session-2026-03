using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache
var cache = builder.AddRedis("cache");

// Add API service with cache reference
var apiService = builder.AddProject<Projects.AIAgent_ApiService>("apiservice")
    .WithReference(cache)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("https", ep => new() { Url = $"{ep.Url}/scalar", DisplayText = "API Explorer", DisplayLocation = UrlDisplayLocation.SummaryAndDetails })
    .WithCommand("demo-data", "Seed Demo Data", async context => 
    {
        var interactionService = context.ServiceProvider.GetRequiredService<IInteractionService>();
        if (interactionService.IsAvailable)
        {
            var result = await interactionService.PromptConfirmationAsync(
                title: "Seed Demo Data",
                message: "This will populate the API with sample weather forecast data. Continue?");

            if (result.Data)
            {
                // In a real scenario, this would call an endpoint or trigger data seeding
                return new ExecuteCommandResult { Success = true };
            }
        }
        return new ExecuteCommandResult { Success = false };
    });

// Add Web frontend
var webfrontend = builder.AddProject<Projects.AIAgent_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(apiService)
    .WithHttpHealthCheck("/health")
    .WaitFor(apiService);

// Add AI Agent Orchestrator - monitors and manages all services
var orchestrator = builder.AddProject<Projects.AIAgent_Orchestrator>("orchestrator")
    .WithReference(apiService)   // Agent discovers and monitors API
    .WithReference(webfrontend)  // Agent discovers and monitors Web
    .WithReference(cache);       // Agent monitors cache

builder.Build().Run();
