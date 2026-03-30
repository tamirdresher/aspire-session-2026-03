var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache
var cache = builder.AddRedis("cache");

// Add API service with cache reference
var apiService = builder.AddProject<Projects.AIAgent_ApiService>("apiservice")
    .WithReference(cache)
    .WithHttpHealthCheck("/health");

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
