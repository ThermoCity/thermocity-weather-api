
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThermoCity.Weather.Application.Interfaces;

namespace ThermoCity.Weather.Worker.Workers;

public class WeatherCollectionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WeatherCollectionWorker> _logger;
    private readonly TimeSpan _collectionInterval;

    public WeatherCollectionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<WeatherCollectionWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Obtém o intervalo de coleta do appsettings.json.
        // Utiliza 5 minutos caso a configuração não esteja definida.
        var intervalMinutes = configuration.GetValue<int>(
            "Worker:WeatherCollectionIntervalMinutes", 5);

        if (intervalMinutes <= 0)
            throw new InvalidOperationException(
                "O intervalo de coleta deve ser maior que zero.");

        _collectionInterval = TimeSpan.FromMinutes(intervalMinutes);
    }

    // Executado automaticamente quando a aplicação Worker inicia.
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "WeatherCollectionWorker iniciado. Intervalo: {Minutes} minutos.",
            _collectionInterval.TotalMinutes);

        // Mantém a rotina ativa até o encerramento da aplicação.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Cria um escopo para resolver serviços registrados como Scoped.
                using var scope = _scopeFactory.CreateScope();

                // Obtém o serviço responsável pelas regras da coleta.
                var collectionService = scope.ServiceProvider
                    .GetRequiredService<IWeatherCollectionService>();

                // Executa a coleta e persiste a leitura no MongoDB.
                await collectionService.CollectAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                // Encerra a rotina quando a aplicação solicita cancelamento.
                break;
            }
            catch (Exception exception)
            {
                // Registra falhas inesperadas sem encerrar o Worker.
                _logger.LogError(
                    exception,
                    "Erro inesperado durante a coleta meteorológica.");
            }

            try
            {
                // Aguarda o intervalo antes de iniciar uma nova coleta.
                await Task.Delay(_collectionInterval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("WeatherCollectionWorker encerrado.");
    }
}
