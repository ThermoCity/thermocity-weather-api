
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Serilog;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Infrastructure.Mongo;
using ThermoCity.Weather.Infrastructure.Mongo.Repositories;
using ThermoCity.Weather.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configura o Serilog como provedor de logging da aplicação.
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

// Registra os controllers responsáveis pelos endpoints REST.
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

// Configura a documentação da API com Swagger.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configura o Rate Limiting global da API.
builder.Services.AddRateLimiter(options =>
{
    // Define o status HTTP retornado quando o limite é excedido.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Aplica um limitador global, separado por endereço IP do cliente.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext =>
        {
            // Obtém o endereço IP da requisição.
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            return RateLimitPartition.GetFixedWindowLimiter(
                // Identifica o grupo de requisições que compartilhará o limite.
                partitionKey: clientIp,

                // Define as regras de limitação para cada grupo.
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    // Renova automaticamente a janela após seu intervalo.
                    AutoReplenishment = true,

                    // Permite até 10 requisições por janela.
                    PermitLimit = 10,

                    // Não mantém requisições excedentes em fila.
                    QueueLimit = 0,

                    // Define a duração de cada janela de limitação.
                    Window = TimeSpan.FromMinutes(1)
                });
        });
});

// Obtém as configurações de conexão com o MongoDB.
var connectionString = builder.Configuration["MongoDb:ConnectionString"]
    ?? throw new InvalidOperationException(
        "MongoDb:ConnectionString não foi configurada.");

var databaseName = builder.Configuration["MongoDb:DatabaseName"]
    ?? throw new InvalidOperationException(
        "MongoDb:DatabaseName não foi configurado.");

// Registra o cliente MongoDB como Singleton.
// O MongoClient é thread-safe e deve ser compartilhado pela aplicação.
builder.Services.AddSingleton<IMongoClient>(
    _ => new MongoClient(connectionString));

// Disponibiliza o banco de dados para os repositórios.
builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(databaseName);
});

// Registra os repositórios utilizados pelos controllers.
builder.Services.AddScoped<IWeatherReadingRepository, WeatherReadingRepository>();
builder.Services.AddScoped<IDailyForecastRepository, DailyForecastRepository>();
builder.Services.AddScoped<IDailyWeatherSummaryRepository, DailyWeatherSummaryRepository>();

// Registra os Health Checks da aplicação.
builder.Services.AddHealthChecks()
    // Verifica se a própria aplicação está em execução.
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" })
    // Verifica a disponibilidade do MongoDB.
    .AddCheck<MongoDbHealthCheck>(
        "mongodb",
        tags: new[] { "ready" });

// Constrói a aplicação.
var app = builder.Build();

// Registra automaticamente informações das requisições HTTP.
app.UseSerilogRequestLogging();

// Disponibiliza Swagger somente no ambiente de desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redireciona requisições HTTP para HTTPS.
app.UseHttpsRedirection();

// Aplica o limite global de requisições.
app.UseRateLimiter();

// Mapeia os endpoints dos controllers.
app.MapControllers();

// Endpoint de liveness: verifica se a API está em execução.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// Endpoint de readiness: verifica se os serviços essenciais,
// incluindo o MongoDB, estão disponíveis.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Inicia a aplicação.
app.Run();

// Permite utilizar WebApplicationFactory nos testes de integração.
public partial class Program { }
