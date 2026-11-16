
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ThermoCity.Weather.Application.Interfaces;

namespace ThermoCity.Weather.Worker.Workers;

public class ForecastCollectionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ForecastCollectionWorker> _logger;
    private readonly IConfiguration _configuration;

    public ForecastCollectionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ForecastCollectionWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue<int>(
            "Worker:ForecastCollectionIntervalMinutes", 5);

        if (intervalMinutes <= 0)
            throw new InvalidOperationException(
                "ForecastCollectionIntervalMinutes deve ser maior que zero.");

        _logger.LogInformation(
            "ForecastCollectionWorker iniciado. Intervalo: {Interval} minutos.",
            intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var forecastService = scope.ServiceProvider
                    .GetRequiredService<IForecastCollectionService>();

                _logger.LogInformation(
                    "Iniciando coleta de previsões meteorológicas.");

                await forecastService.CollectAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Erro durante a coleta de previsões meteorológicas.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromMinutes(intervalMinutes),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("ForecastCollectionWorker finalizado.");
    }
}
