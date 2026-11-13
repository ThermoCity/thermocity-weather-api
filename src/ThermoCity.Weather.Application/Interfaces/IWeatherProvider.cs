using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.Application.Interfaces;

public interface IWeatherProvider
{
    // Obtém as condições climáticas atuais.
    Task<WeatherReading> GetCurrentWeatherAsync(
        CancellationToken cancellationToken);

    // Obtém e organiza a previsão diária dos próximos dias.
    Task<IReadOnlyCollection<DailyForecast>> GetDailyForecastAsync(
        int days,
        CancellationToken cancellationToken);
}