var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache
var cache = builder.AddRedis("cache")
    .WithDataVolume();

// Add PostgreSQL with a database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var appdb = postgres.AddDatabase("appdb");

var apiService = builder.AddProject<Projects.Integrations_ApiService>("apiservice")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(appdb)
    .WaitFor(postgres)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("https", ep => new() { Url = $"{ep.Url}/scalar", DisplayText = "API Explorer", DisplayLocation = UrlDisplayLocation.SummaryAndDetails });

builder.AddProject<Projects.Integrations_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
