
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThermoCity.Weather.Application.Interfaces;

namespace ThermoCity.Weather.Worker.Workers;

public class ForecastCollectionWorker : BackgroundService
{
    private static readonly TimeZoneInfo CityTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ForecastCollectionWorker> _logger;
    private readonly int _collectionHour;
    private readonly int _collectionMinute;

    public ForecastCollectionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ForecastCollectionWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Obtém o horário diário configurado no appsettings.json.
        _collectionHour = configuration.GetValue<int>(
            "Worker:ForecastCollectionHour", 6);

        _collectionMinute = configuration.GetValue<int>(
            "Worker:ForecastCollectionMinute", 0);

        // Valida os valores do horário configurado.
        if (_collectionHour is < 0 or > 23 ||
            _collectionMinute is < 0 or > 59)
        {
            throw new InvalidOperationException(
                "Horário de coleta de previsões inválido.");
        }
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "ForecastCollectionWorker iniciado. Execução diária às {Hour:D2}:{Minute:D2}.",
            _collectionHour,
            _collectionMinute);

        // Mantém o agendamento ativo enquanto a aplicação estiver em execução.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calcula quanto tempo falta para o próximo horário de coleta.
                var delay = GetDelayUntilNextExecution();

                _logger.LogInformation(
                    "Próxima coleta de previsões em {Delay}.",
                    delay);

                // Aguarda até o próximo horário programado.
                await Task.Delay(delay, stoppingToken);

                // Cria um escopo para acessar os serviços Scoped.
                using var scope = _scopeFactory.CreateScope();

                // Obtém o serviço responsável pela coleta de previsões.
                var forecastService = scope.ServiceProvider
                    .GetRequiredService<IForecastCollectionService>();

                // Consulta a OpenWeather e persiste as previsões no MongoDB.
                await forecastService.CollectAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Uma falha não encerra o agendamento dos próximos dias.
                _logger.LogError(
                    exception,
                    "Erro inesperado durante a coleta de previsões.");
            }
        }

        _logger.LogInformation("ForecastCollectionWorker encerrado.");
    }

    // Calcula o tempo restante até a próxima execução no horário local.
    private TimeSpan GetDelayUntilNextExecution()
    {
        var nowUtc = DateTime.UtcNow;

        // Converte o horário UTC para o fuso de Presidente Prudente.
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(
            nowUtc,
            CityTimeZone);

        // Monta o horário programado para hoje.
        var nextExecution = DateTime.SpecifyKind(
            nowLocal.Date.AddHours(_collectionHour)
                .AddMinutes(_collectionMinute),
            DateTimeKind.Unspecified);

        // Se o horário de hoje já passou, agenda para o dia seguinte.
        if (nextExecution <= nowLocal)
            nextExecution = nextExecution.AddDays(1);

        // Converte o horário local programado para UTC.
        var nextExecutionUtc = TimeZoneInfo.ConvertTimeToUtc(
            nextExecution,
            CityTimeZone);

        return nextExecutionUtc - nowUtc;
    }
}
