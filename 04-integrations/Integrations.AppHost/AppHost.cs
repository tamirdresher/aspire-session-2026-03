var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Integrations_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("https", ep => new() { Url = $"{ep.Url}/scalar", DisplayText = "API Explorer", DisplayLocation = UrlDisplayLocation.SummaryAndDetails });

builder.AddProject<Projects.Integrations_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
