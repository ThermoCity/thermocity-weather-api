
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThermoCity.Weather.Application.Interfaces;

namespace ThermoCity.Weather.Worker.Workers;

public class DailyConsolidationWorker : BackgroundService
{
    private static readonly TimeZoneInfo CityTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyConsolidationWorker> _logger;
    private readonly int _consolidationHour;
    private readonly int _consolidationMinute;

    public DailyConsolidationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<DailyConsolidationWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Obtém o horário da consolidação no appsettings.json.
        _consolidationHour = configuration.GetValue<int>(
            "Worker:ConsolidationHour", 1);

        _consolidationMinute = configuration.GetValue<int>(
            "Worker:ConsolidationMinute", 0);

        // Valida o horário configurado.
        if (_consolidationHour is < 0 or > 23 ||
            _consolidationMinute is < 0 or > 59)
        {
            throw new InvalidOperationException(
                "Horário de consolidação inválido.");
        }
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "DailyConsolidationWorker iniciado. Execução diária às {Hour:D2}:{Minute:D2}.",
            _consolidationHour,
            _consolidationMinute);

        // Mantém o agendamento ativo até o encerramento da aplicação.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calcula o tempo até o próximo horário de consolidação.
                var delay = GetDelayUntilNextExecution();

                _logger.LogInformation(
                    "Próxima consolidação em {Delay}.",
                    delay);

                // Aguarda até o horário programado.
                await Task.Delay(delay, stoppingToken);

                // Cria um escopo para resolver o serviço Scoped.
                using var scope = _scopeFactory.CreateScope();

                // Obtém o serviço responsável pela consolidação diária.
                var consolidationService = scope.ServiceProvider
                    .GetRequiredService<IDailyConsolidationService>();

                // Consolida as observações do dia anterior.
                await consolidationService.ConsolidatePreviousDayAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Registra a falha e mantém o agendamento para o próximo dia.
                _logger.LogError(
                    exception,
                    "Erro inesperado durante a consolidação diária.");
            }
        }

        _logger.LogInformation("DailyConsolidationWorker encerrado.");
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
            nowLocal.Date.AddHours(_consolidationHour)
                .AddMinutes(_consolidationMinute),
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
