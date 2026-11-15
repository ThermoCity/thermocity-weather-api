
using MongoDB.Driver;
using Serilog;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Services;
using ThermoCity.Weather.Infrastructure.Mongo.Repositories;
using ThermoCity.Weather.Infrastructure.OpenWeather;
using ThermoCity.Weather.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Configura o Serilog como provedor de logging do Worker.
builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ThermoCity.Weather.Worker")
        .Enrich.WithProperty(
            "Environment",
            builder.Environment.EnvironmentName)
        .WriteTo.Console();
});

// Configura o MongoDB.
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

// Repositórios MongoDB.
builder.Services.AddScoped<IWeatherReadingRepository, WeatherReadingRepository>();
builder.Services.AddScoped<IDailyForecastRepository, DailyForecastRepository>();
builder.Services.AddScoped<IDailyWeatherSummaryRepository, DailyWeatherSummaryRepository>();

// Serviço de cálculo do índice de calor.
builder.Services.AddScoped<IHeatIndexService, HeatIndexService>();

// Configura o provedor OpenWeather.
var openWeatherBaseUrl =
    builder.Configuration["OpenWeather:BaseUrl"]
    ?? throw new InvalidOperationException(
        "OpenWeather:BaseUrl não foi configurada.");

builder.Services.AddHttpClient<IWeatherProvider, OpenWeatherProvider>(
    client =>
    {
        client.BaseAddress = new Uri(openWeatherBaseUrl);
    });

// Configura a quantidade de dias de previsão.
var forecastDays = builder.Configuration.GetValue<int>(
    "Worker:ForecastDays", 3);

// Serviço de coleta das condições climáticas atuais.
builder.Services.AddScoped<
    IWeatherCollectionService,
    WeatherCollectionService>();

// Serviço de coleta das previsões meteorológicas.
builder.Services.AddScoped<IForecastCollectionService>(serviceProvider =>
{
    return new ForecastCollectionService(
        serviceProvider.GetRequiredService<IWeatherProvider>(),
        serviceProvider.GetRequiredService<IDailyForecastRepository>(),
        serviceProvider.GetRequiredService<IHeatIndexService>(),
        serviceProvider.GetRequiredService<
            Microsoft.Extensions.Logging.ILogger<ForecastCollectionService>>(),
        forecastDays);
});

// Serviço de consolidação diária.
builder.Services.AddScoped<
    IDailyConsolidationService,
    DailyConsolidationService>();

// Registra as rotinas executadas em segundo plano.
builder.Services.AddHostedService<WeatherCollectionWorker>();
builder.Services.AddHostedService<ForecastCollectionWorker>();
builder.Services.AddHostedService<DailyConsolidationWorker>();

// Constrói e inicia o Worker.
var host = builder.Build();

await host.RunAsync();
