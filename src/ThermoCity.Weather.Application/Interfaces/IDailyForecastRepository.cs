
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Application.Interfaces;

public interface IDailyForecastRepository
{
    Task UpsertManyAsync(
        IEnumerable<DailyForecast> forecasts,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DailyForecast>> GetByPeriodAsync(
        WeatherSource source,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);
}
