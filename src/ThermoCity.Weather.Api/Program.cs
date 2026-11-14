
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Serilog;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Infrastructure.Mongo;
using ThermoCity.Weather.Infrastructure.Mongo.Repositories;
using ThermoCity.Weather.Infrastructure.PostgreSql;
using ThermoCity.Weather.Infrastructure.PostgreSql.Repositories;
using ThermoCity.Weather.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configura o Serilog.
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ThermoCity.Weather.Api")
        .Enrich.WithProperty(
            "Environment",
            context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console();
});

// Registra os controllers.
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

// Configura o Swagger.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura o Rate Limiting.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientIp,
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1)
                });
        });
});

// Configuração do MongoDB.
var mongoConnectionString =
    builder.Configuration["MongoDb:ConnectionString"]
    ?? throw new InvalidOperationException(
        "MongoDb:ConnectionString não foi configurada.");

var mongoDatabaseName =
    builder.Configuration["MongoDb:DatabaseName"]
    ?? throw new InvalidOperationException(
        "MongoDb:DatabaseName não foi configurado.");

builder.Services.AddSingleton<IMongoClient>(
    _ => new MongoClient(mongoConnectionString));

builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
});

// Configuração do PostgreSQL.
var postgresConnectionString =
    builder.Configuration.GetConnectionString("PostgreSql")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:PostgreSql não foi configurada.");

builder.Services.AddDbContext<ThermoCityDbContext>(options =>
    options.UseNpgsql(postgresConnectionString));

// Seleciona o banco utilizado nas consultas da API.
var readProvider =
    builder.Configuration["Database:ReadProvider"] ?? "MongoDB";

switch (readProvider.ToUpperInvariant())
{
    case "MONGODB":
        builder.Services.AddScoped<
            IWeatherReadingRepository,
            WeatherReadingRepository>();

        builder.Services.AddScoped<
            IDailyForecastRepository,
            DailyForecastRepository>();

        builder.Services.AddScoped<
            IDailyWeatherSummaryRepository,
            DailyWeatherSummaryRepository>();
        break;

    case "POSTGRESQL":
        builder.Services.AddScoped<
            IWeatherReadingRepository,
            PostgresWeatherReadingRepository>();

        builder.Services.AddScoped<
            IDailyForecastRepository,
            PostgresDailyForecastRepository>();

        builder.Services.AddScoped<
            IDailyWeatherSummaryRepository,
            PostgresDailyWeatherSummaryRepository>();
        break;

    default:
        throw new InvalidOperationException(
            $"Provedor de leitura inválido: {readProvider}");
}

// Health Checks.
builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" })
    .AddCheck<MongoDbHealthCheck>(
        "mongodb",
        tags: new[] { "ready" });

// Constrói a aplicação.
var app = builder.Build();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

public partial class Program { }
