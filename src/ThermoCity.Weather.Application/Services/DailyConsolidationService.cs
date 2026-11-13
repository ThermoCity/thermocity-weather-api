
using Microsoft.Extensions.Logging;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Application.Services;

public class DailyConsolidationService : IDailyConsolidationService
{
    private readonly IWeatherReadingRepository _readingRepository;
    private readonly IDailyWeatherSummaryRepository _summaryRepository;
    private readonly ILogger<DailyConsolidationService> _logger;

    private static readonly TimeZoneInfo CityTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public DailyConsolidationService(
        IWeatherReadingRepository readingRepository,
        IDailyWeatherSummaryRepository summaryRepository,
        ILogger<DailyConsolidationService> logger)
    {
        _readingRepository = readingRepository;
        _summaryRepository = summaryRepository;
        _logger = logger;
    }

    public async Task ConsolidatePreviousDayAsync(
        CancellationToken cancellationToken)
    {
        // Identifica o dia anterior no horário local da cidade.
        var localToday = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            CityTimeZone).Date;

        var referenceDate = localToday.AddDays(-1);

        // Converte o início e o fim do dia local para UTC.
        var startDate = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(referenceDate, DateTimeKind.Unspecified),
            CityTimeZone);

        var endDate = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(referenceDate.AddDays(1), DateTimeKind.Unspecified),
            CityTimeZone);

        foreach (var source in Enum.GetValues<WeatherSource>())
        {
            var readings = await _readingRepository.GetByPeriodAsync(
                source,
                startDate,
                endDate,
                cancellationToken);

            var validReadings = readings
                .Where(reading => reading.Status == ReadingStatus.Complete)
                .ToList();

            if (validReadings.Count == 0)
            {
                _logger.LogWarning(
                    "Nenhuma observação válida encontrada para {Source} em {Date}.",
                    source,
                    referenceDate);

                continue;
            }

            var temperatures = validReadings
                .Where(reading => reading.TemperatureCelsius.HasValue)
                .Select(reading => reading.TemperatureCelsius!.Value)
                .ToList();

            var humidities = validReadings
                .Where(reading => reading.HumidityPercentage.HasValue)
                .Select(reading => reading.HumidityPercentage!.Value)
                .ToList();

            var heatIndexes = validReadings
                .Where(reading => reading.HeatIndexCelsius.HasValue)
                .Select(reading => reading.HeatIndexCelsius!.Value)
                .ToList();

            var summary = new DailyWeatherSummary
            {
                Source = source,
                ReferenceDate = referenceDate,

                MinimumTemperatureCelsius = temperatures.Min(),
                AverageTemperatureCelsius = temperatures.Average(),
                MaximumTemperatureCelsius = temperatures.Max(),

                MinimumHumidityPercentage = humidities.Min(),
                AverageHumidityPercentage = humidities.Average(),
                MaximumHumidityPercentage = humidities.Max(),

                MinimumHeatIndexCelsius = heatIndexes.Count > 0
                    ? heatIndexes.Min()
                    : null,

                AverageHeatIndexCelsius = heatIndexes.Count > 0
                    ? heatIndexes.Average()
                    : null,

                MaximumHeatIndexCelsius = heatIndexes.Count > 0
                    ? heatIndexes.Max()
                    : null,

                ObservationCount = validReadings.Count,
                ConsolidationDate = DateTime.UtcNow
            };

            await _summaryRepository.UpsertAsync(
                summary,
                cancellationToken);

            _logger.LogInformation(
                "Resumo diário consolidado. Fonte: {Source}. Data: {Date}. Observações: {Count}.",
                source,
                referenceDate,
                validReadings.Count);
        }
    }
}
