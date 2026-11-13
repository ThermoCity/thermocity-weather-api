
using Microsoft.Extensions.Logging;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Application.Services;

public class WeatherCollectionService : IWeatherCollectionService
{
    private readonly IWeatherProvider _weatherProvider;
    private readonly IWeatherReadingRepository _readingRepository;
    private readonly IHeatIndexService _heatIndexService;
    private readonly ILogger<WeatherCollectionService> _logger;

    // Limites físicos amplos para identificar valores meteorológicos inválidos.
    private const double MinimumTemperature = -90;
    private const double MaximumTemperature = 60;

    public WeatherCollectionService(
        IWeatherProvider weatherProvider,
        IWeatherReadingRepository readingRepository,
        IHeatIndexService heatIndexService,
        ILogger<WeatherCollectionService> logger)
    {
        _weatherProvider = weatherProvider;
        _readingRepository = readingRepository;
        _heatIndexService = heatIndexService;
        _logger = logger;
    }

    public async Task CollectAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Obtém a observação meteorológica atual da OpenWeather.
            var reading = await _weatherProvider.GetCurrentWeatherAsync(
                cancellationToken);

            // Verifica se a temperatura está dentro de uma faixa plausível.
            if (reading.TemperatureCelsius is double temperature &&
                (!double.IsFinite(temperature) ||
                 temperature < MinimumTemperature ||
                 temperature > MaximumTemperature))
            {
                _logger.LogWarning(
                    "Leitura descartada: temperatura inválida ({Temperature} °C).",
                    temperature);

                return;
            }

            // Verifica se a umidade está dentro do intervalo permitido.
            if (reading.HumidityPercentage is double humidity &&
                (!double.IsFinite(humidity) ||
                 humidity < 0 ||
                 humidity > 100))
            {
                _logger.LogWarning(
                    "Leitura descartada: umidade inválida ({Humidity}%).",
                    humidity);

                return;
            }

            // Sem temperatura ou umidade, a leitura é armazenada como parcial.
            if (reading.TemperatureCelsius is null ||
                reading.HumidityPercentage is null)
            {
                reading.Status = ReadingStatus.Partial;
                reading.HeatIndexCelsius = null;

                _logger.LogWarning(
                    "Leitura parcial recebida da OpenWeather. " +
                    "Temperatura ou umidade não disponível.");
            }
            else
            {
                // Calcula o índice de calor quando os dois valores estão disponíveis.
                reading.HeatIndexCelsius = _heatIndexService.Calculate(
                    reading.TemperatureCelsius.Value,
                    reading.HumidityPercentage.Value);

                reading.Status = ReadingStatus.Complete;
            }

            // Persiste a observação no MongoDB.
            await _readingRepository.UpsertAsync(
                reading,
                cancellationToken);

            _logger.LogInformation(
                "Observação meteorológica processada. Fonte: {Source}. Status: {Status}.",
                reading.Source,
                reading.Status);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Respeita o encerramento solicitado pela aplicação.
            throw;
        }
        catch (Exception exception)
        {
            // Registra a falha sem impedir as próximas tentativas do Worker.
            _logger.LogError(
                exception,
                "Falha ao coletar observação meteorológica da OpenWeather.");
        }
    }
}
