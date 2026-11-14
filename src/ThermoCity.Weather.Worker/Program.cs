
using MongoDB.Driver;
using Serilog;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Services;
using ThermoCity.Weather.Infrastructure.Mongo.Repositories;
using ThermoCity.Weather.Infrastructure.OpenWeather;
using ThermoCity.Weather.Infrastructure.Repositories;
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

// Obtém as configurações de conexão com o MongoDB.
var connectionString = builder.Configuration["MongoDb:ConnectionString"]
    ?? throw new InvalidOperationException(
        "MongoDb:ConnectionString não foi configurada.");

var databaseName = builder.Configuration["MongoDb:DatabaseName"]
    ?? throw new InvalidOperationException(
        "MongoDb:DatabaseName não foi configurado.");

// Registra o cliente MongoDB como Singleton.
// O mesmo cliente pode ser compartilhado por todos os serviços.
builder.Services.AddSingleton<IMongoClient>(
    _ => new MongoClient(connectionString));

// Disponibiliza o banco de dados para os repositórios.
builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});

// Registra os repositórios responsáveis pela persistência.
builder.Services.AddScoped<IWeatherReadingRepository, WeatherReadingRepository>();
builder.Services.AddScoped<IDailyForecastRepository, DailyForecastRepository>();
builder.Services.AddScoped<IDailyWeatherSummaryRepository, DailyWeatherSummaryRepository>();

// Registra o serviço responsável pelo cálculo do índice de calor.
builder.Services.AddScoped<IHeatIndexService, HeatIndexService>();

// Obtém a URL base da API OpenWeather.
var openWeatherBaseUrl = builder.Configuration["OpenWeather:BaseUrl"]
    ?? throw new InvalidOperationException(
        "OpenWeather:BaseUrl não foi configurada.");

// Configura o HttpClient utilizado pelo provedor meteorológico.
// A implementação OpenWeatherProvider recebe HttpClient e IConfiguration.
builder.Services.AddHttpClient<IWeatherProvider, OpenWeatherProvider>(
    client =>
    {
        client.BaseAddress = new Uri(openWeatherBaseUrl);
    });

// Registra o serviço de coleta das condições climáticas atuais.
builder.Services.AddScoped<
    IWeatherCollectionService,
    WeatherCollectionService>();

// Obtém a quantidade de dias de previsão configurada.
var forecastDays = builder.Configuration.GetValue<int>(
    "Worker:ForecastDays", 3);

// Registra o serviço de coleta de previsões.
// A fábrica permite passar a configuração forecastDays ao construtor.
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

// Registra o serviço de consolidação diária.
builder.Services.AddScoped<
    IDailyConsolidationService,
    DailyConsolidationService>();

// Registra as três rotinas executadas em segundo plano.
builder.Services.AddHostedService<WeatherCollectionWorker>();
builder.Services.AddHostedService<ForecastCollectionWorker>();
builder.Services.AddHostedService<DailyConsolidationWorker>();

// Constrói e inicia o Worker.
var host = builder.Build();

await host.RunAsync();
