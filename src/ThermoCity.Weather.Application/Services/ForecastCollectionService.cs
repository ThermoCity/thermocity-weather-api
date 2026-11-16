
using Microsoft.Extensions.Logging;
using ThermoCity.Weather.Application.Interfaces;

namespace ThermoCity.Weather.Application.Services;

public class ForecastCollectionService : IForecastCollectionService
{
    private readonly IWeatherProvider _weatherProvider;
    private readonly IDailyForecastRepository _forecastRepository;
    private readonly IHeatIndexService _heatIndexService;
    private readonly ILogger<ForecastCollectionService> _logger;

    private readonly int _forecastDays;

    public ForecastCollectionService(
        IWeatherProvider weatherProvider,
        IDailyForecastRepository forecastRepository,
        IHeatIndexService heatIndexService,
        ILogger<ForecastCollectionService> logger,
        int forecastDays)
    {
        _weatherProvider = weatherProvider;
        _forecastRepository = forecastRepository;
        _heatIndexService = heatIndexService;
        _logger = logger;
        _forecastDays = forecastDays;
    }

    public async Task CollectAsync(CancellationToken cancellationToken)
    {
        try
        {
            var forecasts = await _weatherProvider.GetDailyForecastAsync(
                _forecastDays,
                cancellationToken);

            foreach (var forecast in forecasts)
            {
                if (forecast.DayTemperatureCelsius is double temperature &&
                    forecast.HumidityPercentage is double humidity)
                {
                    forecast.EstimatedHeatIndexCelsius =
                        _heatIndexService.Calculate(temperature, humidity);
                }
                else
                {
                    forecast.EstimatedHeatIndexCelsius = null;
                }
            }

            await _forecastRepository.UpsertManyAsync(
                forecasts,
                cancellationToken);

            _logger.LogInformation(
                "Coleta de previsões concluída. Registros processados: {Count}.",
                forecasts.Count);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Falha ao coletar previsões meteorológicas.");
        }
    }
}
