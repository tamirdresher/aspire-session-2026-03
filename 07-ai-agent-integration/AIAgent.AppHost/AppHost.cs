using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// Azure OpenAI configuration — set via `dotnet user-secrets` locally,
// or via Aspire parameters in production.
// dotnet user-secrets set azure-openai-endpoint "https://<your-resource>.openai.azure.com/"
// dotnet user-secrets set azure-openai-deployment "gpt-4o-mini"
var azureOpenAIEndpoint   = builder.AddParameter("azure-openai-endpoint",   secret: true);
var azureOpenAIDeployment = builder.AddParameter("azure-openai-deployment",  defaultValue: "gpt-4o-mini");

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

// Add AI Agent Orchestrator — uses Microsoft Agent Framework GA (1.0.0)
// to intelligently monitor all services with LLM-powered analysis.
var orchestrator = builder.AddProject<Projects.AIAgent_Orchestrator>("orchestrator")
    .WithReference(apiService)
    .WithReference(webfrontend)
    .WithReference(cache)
    .WithEnvironment("AZURE_OPENAI_ENDPOINT",         azureOpenAIEndpoint)
    .WithEnvironment("AZURE_OPENAI_DEPLOYMENT_NAME",  azureOpenAIDeployment);

builder.Build().Run();
