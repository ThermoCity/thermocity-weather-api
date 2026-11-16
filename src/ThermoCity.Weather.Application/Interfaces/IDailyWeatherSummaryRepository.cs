
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Application.Interfaces;

public interface IDailyWeatherSummaryRepository
{
    Task UpsertAsync(
        DailyWeatherSummary summary,
        CancellationToken cancellationToken);

    Task<DailyWeatherSummary?> GetByDateAsync(
        WeatherSource source,
        DateTime referenceDate,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DailyWeatherSummary>> GetByPeriodAsync(
        WeatherSource source,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);
}
