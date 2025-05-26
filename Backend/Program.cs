using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Data;
using Data.Repository;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddDbContext<EvnContext>((serviceProvider, options) =>
    {

        string? connectionString = Environment.GetEnvironmentVariable("DbConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            // Throw an exception if the connection string is not found, to fail fast during startup
            throw new InvalidOperationException("SQL connection string 'SqlConnectionString' is not configured.");
        }
        options.UseSqlServer(connectionString);
    })
    .AddControllers().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddHttpClient("ExternalApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();

builder.Build().Run();
