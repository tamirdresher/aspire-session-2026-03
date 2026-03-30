using System.Diagnostics;
using System.Diagnostics.Metrics;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// --- Observability Demo: Custom ActivitySource and Meter ---
var activitySource = new ActivitySource("Observability.ApiService");
var meter = new Meter("Observability.ApiService");
var requestCounter = meter.CreateCounter<long>("api.requests.count", description: "Counts the number of API requests");
var slowRequestDuration = meter.CreateHistogram<double>("api.slow_request.duration", unit: "ms", description: "Duration of slow requests");

// Register the custom ActivitySource and Meter with OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("Observability.ApiService"))
    .WithMetrics(metrics => metrics.AddMeter("Observability.ApiService"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "weatherforecast"));

    // Structured logging with named parameters
    logger.LogInformation("Generating weather forecast with {Count} items at {RequestTime}", 5, DateTime.UtcNow);

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    logger.LogInformation("Weather forecast generated successfully with summaries: {Summaries}",
        string.Join(", ", forecast.Select(f => f.Summary)));

    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/slow", async (ILogger<Program> logger) =>
{
    requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "slow"));

    // Custom span to demonstrate distributed tracing
    using var activity = activitySource.StartActivity("SlowOperation", ActivityKind.Internal);

    var delayMs = Random.Shared.Next(500, 3000);
    activity?.SetTag("delay.ms", delayMs);
    activity?.SetTag("delay.reason", "simulated-slow-operation");

    logger.LogInformation("Starting slow operation with {DelayMs}ms delay", delayMs);

    // Simulate a slow database or external service call
    using (var dbActivity = activitySource.StartActivity("SimulatedDatabaseQuery"))
    {
        dbActivity?.SetTag("db.system", "simulated");
        dbActivity?.SetTag("db.statement", "SELECT * FROM weather_data WHERE region = 'demo'");
        await Task.Delay(delayMs / 2);
    }

    // Simulate processing
    using (var processActivity = activitySource.StartActivity("ProcessResults"))
    {
        processActivity?.SetTag("processing.items", 42);
        await Task.Delay(delayMs / 2);
    }

    slowRequestDuration.Record(delayMs);

    logger.LogInformation("Slow operation completed in {ElapsedMs}ms", delayMs);

    return Results.Ok(new { message = "Slow operation completed", delayMs });
})
.WithName("SlowEndpoint");

app.MapGet("/error", (ILogger<Program> logger) =>
{
    requestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "error"));

    logger.LogWarning("About to execute error endpoint - this will throw an exception");

    throw new InvalidOperationException("This is a demo exception to show error tracing in the Aspire dashboard!");
})
.WithName("ErrorEndpoint");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
