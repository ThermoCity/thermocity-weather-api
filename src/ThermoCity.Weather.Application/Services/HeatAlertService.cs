using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Models;
using ThermoCity.Weather.Domain;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Application.Services;

public class HeatAlertService : IHeatAlertService
{
    private const string ThresholdVersion = "A707-2003-2026-v1";

    private static readonly TimeZoneInfo BrazilTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private readonly IWeatherReadingRepository _readingRepository;
    private readonly IDailyForecastRepository _forecastRepository;

    public HeatAlertService(
        IWeatherReadingRepository readingRepository,
        IDailyForecastRepository forecastRepository)
    {
        _readingRepository = readingRepository;
        _forecastRepository = forecastRepository;
    }

    public async Task<HeatAlertResult> GetHeatAlertsAsync(
        CancellationToken cancellationToken)
    {
        var now = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            BrazilTimeZone);

        var today = DateOnly.FromDateTime(now.DateTime);

        // Busca as leituras dos últimos três dias para avaliar
        // as regras de temperaturas elevadas consecutivas.
        var readingStart = ToUtc(today.AddDays(-2));
        var readingEnd = ToUtc(today.AddDays(1));

        // Busca as previsões dos próximos dias.
        var forecastStart = today.ToDateTime(TimeOnly.MinValue);
        var forecastEnd = today.AddDays(4).ToDateTime(TimeOnly.MinValue);

        var latestReadingTask = _readingRepository.GetLatestAsync(
            WeatherSource.OpenWeather,
            cancellationToken);

        var readingsTask = _readingRepository.GetByPeriodAsync(
            WeatherSource.OpenWeather,
            readingStart,
            readingEnd,
            cancellationToken);

        var forecastsTask = _forecastRepository.GetByPeriodAsync(
            WeatherSource.OpenWeather,
            forecastStart,
            forecastEnd,
            cancellationToken);

        await Task.WhenAll(
            latestReadingTask,
            readingsTask,
            forecastsTask);

        var latestReading = await latestReadingTask;
        var readings = await readingsTask;
        var forecasts = await forecastsTask;

        // Calcula o maior índice de calor observado em cada dia.
        var dailyHeatIndexes = readings
            .Where(reading =>
                reading.Status == ReadingStatus.Complete &&
                reading.HeatIndexCelsius.HasValue)
            .GroupBy(reading =>
                DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTime(
                        new DateTimeOffset(
                            DateTime.SpecifyKind(
                                reading.MeasurementDate,
                                DateTimeKind.Utc)),
                        BrazilTimeZone).DateTime))
            .ToDictionary(
                group => group.Key,
                group => (double?)group.Max(
                    reading => reading.HeatIndexCelsius!.Value));

        // Para cada dia, considera a emissão mais recente da previsão.
        var dailyForecasts = forecasts
            .Where(forecast =>
                DateOnly.FromDateTime(forecast.ForecastDate) > today)
            .GroupBy(forecast =>
                DateOnly.FromDateTime(forecast.ForecastDate))
            .Select(group => group
                .OrderByDescending(forecast => forecast.ForecastEmissionDate)
                .First())
            .OrderBy(forecast => forecast.ForecastDate)
            .ToList();

        dailyHeatIndexes.TryGetValue(today, out var todayHeatIndex);

        // Hoje utiliza somente dados observados.
        var todayAlert = todayHeatIndex.HasValue
            ? CreateAlertDay(
                today,
                todayHeatIndex,
                dailyHeatIndexes,
                "OBSERVADO")
            : null;

        var upcomingDays = new List<HeatAlertDayResult>();

        // As previsões são processadas em ordem cronológica.
        // Isso permite avaliar também as regras de três dias consecutivos.
        foreach (var forecast in dailyForecasts)
        {
            var date = DateOnly.FromDateTime(forecast.ForecastDate);
            var heatIndex = forecast.EstimatedHeatIndexCelsius;

            dailyHeatIndexes[date] = heatIndex;

            upcomingDays.Add(CreateAlertDay(
                date,
                heatIndex,
                dailyHeatIndexes,
                "PREVISTO"));
        }

        var classification = todayAlert is null
            ? (
                Criterion: "Sem dados meteorológicos disponíveis",
                Action: "Aguardando leitura meteorológica"
              )
            : GetBusinessRules(
                todayAlert.Level!.Value,
                todayHeatIndex!.Value);

        return new HeatAlertResult
        {
            Today = todayAlert,
            UpcomingDays = upcomingDays,
            Criterion = classification.Criterion,
            ExpectedAction = classification.Action,
            ThresholdVersion = ThresholdVersion,

            UpdatedAt = latestReading is null
                ? now
                : new DateTimeOffset(
                    DateTime.SpecifyKind(
                        latestReading.CollectionDate,
                        DateTimeKind.Utc)),

            // Provisório: indica ausência de leituras.
            // O documento do MVP ainda não define o limite de obsolescência.
            IsStale = latestReading is null
        };
    }

    private static HeatAlertDayResult CreateAlertDay(
        DateOnly date,
        double? heatIndex,
        Dictionary<DateOnly, double?> dailyHeatIndexes,
        string origin)
    {
        if (!heatIndex.HasValue)
        {
            return new HeatAlertDayResult
            {
                Date = date,
                Origin = origin
            };
        }

        var level = CalculateLevel(
            date,
            heatIndex.Value,
            dailyHeatIndexes);

        return new HeatAlertDayResult
        {
            Date = date,
            Level = level,
            Name = GetLevelName(level),
            MaximumHeatIndexCelsius = Math.Round(heatIndex.Value, 1),
            Origin = origin
        };
    }

    private static int CalculateLevel(
        DateOnly date,
        double heatIndex,
        Dictionary<DateOnly, double?> dailyHeatIndexes)
    {
        if (heatIndex >= 38.5 ||
            HasConsecutiveDays(date, 37.0, dailyHeatIndexes))
            return 5;

        if (heatIndex >= 37.0 ||
            HasConsecutiveDays(date, 36.0, dailyHeatIndexes))
            return 4;

        if (heatIndex >= 36.0 ||
            HasConsecutiveDays(date, 35.0, dailyHeatIndexes))
            return 3;

        if (heatIndex >= 35.0)
            return 2;

        return 1;
    }

    private static bool HasConsecutiveDays(
        DateOnly date,
        double threshold,
        Dictionary<DateOnly, double?> dailyHeatIndexes)
    {
        for (var offset = 0; offset < 3; offset++)
        {
            var targetDate = date.AddDays(-offset);

            if (!dailyHeatIndexes.TryGetValue(targetDate, out var value) ||
                !value.HasValue ||
                value.Value < threshold)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetLevelName(int level)
    {
        return level switch
        {
            1 => "Normal",
            2 => "Atenção",
            3 => "Alerta",
            4 => "Alto risco",
            5 => "Emergência",
            _ => "Indefinido"
        };
    }

    private static (string Criterion, string Action) GetBusinessRules(
        int level,
        double heatIndex)
    {
        var criterion = level switch
        {
            5 when heatIndex >= 38.5 =>
                "IC máx. ≥ 38,5 °C",

            5 =>
                "IC máx. ≥ 37,0 °C por 3 dias consecutivos",

            4 when heatIndex >= 37.0 =>
                "IC máx. ≥ 37,0 °C",

            4 =>
                "IC máx. ≥ 36,0 °C por 3 dias consecutivos",

            3 when heatIndex >= 36.0 =>
                "IC máx. ≥ 36,0 °C",

            3 =>
                "IC máx. ≥ 35,0 °C por 3 dias consecutivos",

            2 =>
                "IC máx. ≥ 35,0 °C",

            _ =>
                "IC máx. < 35,0 °C"
        };

        var action = level switch
        {
            1 => "Rotina; o painel apenas monitora.",

            2 => "Alerta no painel e orientação preventiva sobre hidratação e horários.",

            3 => "Começa a busca ativa: idosos 70+ nos setores de maior vulnerabilidade; divulgar refúgios.",

            4 => "Busca ativa ampliada a todos os 60+ dos setores IPVS 4 a 6; UBS em alerta; refúgios abertos.",

            5 => "Busca ativa na cidade toda, com visita presencial aos prioritários; refúgios em horário estendido.",

            _ => string.Empty
        };

        return (criterion, action);
    }

    private static DateTime ToUtc(DateOnly date)
    {
        var localDate = date.ToDateTime(TimeOnly.MinValue);

        return TimeZoneInfo.ConvertTimeToUtc(
            localDate,
            BrazilTimeZone);
    }
}