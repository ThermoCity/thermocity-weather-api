
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Application.Interfaces;

public interface IWeatherReadingRepository
{
    Task UpsertAsync(
        WeatherReading reading,
        CancellationToken cancellationToken);

    Task<WeatherReading?> GetLatestAsync(
        WeatherSource source,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WeatherReading>> GetByPeriodAsync(
        WeatherSource source,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);
}
