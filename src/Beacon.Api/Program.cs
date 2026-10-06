using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using Beacon.Infrastructure;
using Beacon.Api.Endpoints;
using Beacon.Application;
using Beacon.Contracts;
using Beacon.Api.Errors;
using Beacon.Api.RateLimiting;
using Beacon.Api.Health;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConflictExceptionHandler>();
builder.Services.AddExceptionHandler<AiUnavailableExceptionHandler>();
builder.Services.AddBeaconRateLimiting();
builder.Services.AddContracts();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapDevEndpoints();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions 
{ 
    Predicate = c => c.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteJson
});

app.MapApplicationEndpoints();
app.MapDocumentEndpoints();
app.MapSearchEndpoints();
app.MapAskEndpoints();

app.Run();

public partial class Program;