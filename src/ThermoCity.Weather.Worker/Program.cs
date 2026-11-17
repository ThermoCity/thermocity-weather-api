
using MongoDB.Driver;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Services;
using ThermoCity.Weather.Infrastructure.Mongo.Repositories;
using ThermoCity.Weather.Infrastructure.OpenWeather;
using ThermoCity.Weather.Infrastructure.RabbitMQ;
using ThermoCity.Weather.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// ── Serilog ───────────────────────────────────────────────────────────────────
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ThermoCity.Weather.Worker")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console();
});

// ── MongoDB ───────────────────────────────────────────────────────────────────
var mongoConnectionString =
    builder.Configuration["MongoDb:ConnectionString"]
    ?? throw new InvalidOperationException("MongoDb:ConnectionString não foi configurada.");

var mongoDatabaseName =
    builder.Configuration["MongoDb:DatabaseName"]
    ?? throw new InvalidOperationException("MongoDb:DatabaseName não foi configurado.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));

builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));

// ── Repositórios MongoDB ──────────────────────────────────────────────────────
builder.Services.AddScoped<IWeatherReadingRepository, WeatherReadingRepository>();
builder.Services.AddScoped<IDailyForecastRepository, DailyForecastRepository>();
builder.Services.AddScoped<IDailyWeatherSummaryRepository, DailyWeatherSummaryRepository>();

// ── Serviços de negócio ───────────────────────────────────────────────────────
builder.Services.AddScoped<IHeatIndexService, HeatIndexService>();

// ── OpenWeather ───────────────────────────────────────────────────────────────
var openWeatherBaseUrl =
    builder.Configuration["OpenWeather:BaseUrl"]
    ?? throw new InvalidOperationException("OpenWeather:BaseUrl não foi configurada.");

builder.Services.AddHttpClient<IWeatherProvider, OpenWeatherProvider>(client =>
{
    client.BaseAddress = new Uri(openWeatherBaseUrl);
});

// ── Forecast days ─────────────────────────────────────────────────────────────
var forecastDays = builder.Configuration.GetValue<int>("Worker:ForecastDays", 3);

// ── Serviços de coleta ────────────────────────────────────────────────────────
builder.Services.AddScoped<IWeatherCollectionService, WeatherCollectionService>();

builder.Services.AddScoped<IForecastCollectionService>(sp =>
    new ForecastCollectionService(
        sp.GetRequiredService<IWeatherProvider>(),
        sp.GetRequiredService<IDailyForecastRepository>(),
        sp.GetRequiredService<IHeatIndexService>(),
        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ForecastCollectionService>>(),
        forecastDays));

builder.Services.AddScoped<IDailyConsolidationService, DailyConsolidationService>();

// ── RabbitMQ ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IHeatAlertPublisher, RabbitMqHeatAlertPublisher>();

// ── Heat Alert API ────────────────────────────────────────────────────────────
var heatAlertsApiBaseUrl =
    builder.Configuration["HeatAlertsApi:BaseUrl"]
    ?? throw new InvalidOperationException("HeatAlertsApi:BaseUrl não foi configurada.");

builder.Services.AddHttpClient("HeatAlertsApi", client =>
{
    client.BaseAddress = new Uri(heatAlertsApiBaseUrl);
});

// ── Workers ───────────────────────────────────────────────────────────────────
builder.Services.AddHostedService<WeatherCollectionWorker>();
builder.Services.AddHostedService<ForecastCollectionWorker>();
builder.Services.AddHostedService<DailyConsolidationWorker>();
builder.Services.AddHostedService<HeatAlertMonitoringWorker>();

// ── OpenTelemetry ─────────────────────────────────────────────────────────────
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "ThermoCity.Weather.Worker",
            serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// ── Run ───────────────────────────────────────────────────────────────────────
var host = builder.Build();

await host.RunAsync();
