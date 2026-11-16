
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
        var hour = _configuration.GetValue<int>(
            "Worker:ForecastCollectionHour", 6);

        var minute = _configuration.GetValue<int>(
            "Worker:ForecastCollectionMinute", 0);

        if (hour is < 0 or > 23 || minute is < 0 or > 59)
            throw new InvalidOperationException(
                "Horário de coleta de previsões inválido.");

        _logger.LogInformation(
            "ForecastCollectionWorker iniciado. Horário diário: {Hour:D2}:{Minute:D2}.",
            hour, minute);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextExecution = now.Date.AddHours(hour).AddMinutes(minute);

            if (nextExecution <= now)
                nextExecution = nextExecution.AddDays(1);

            var delay = nextExecution - now;

            _logger.LogInformation(
                "Próxima coleta de previsões agendada para {NextExecution}.",
                nextExecution);

            try
            {
                await Task.Delay(delay, stoppingToken);

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
        }

        _logger.LogInformation("ForecastCollectionWorker finalizado.");
    }
}
