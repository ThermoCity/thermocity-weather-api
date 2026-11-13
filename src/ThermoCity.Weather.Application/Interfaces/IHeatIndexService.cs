
namespace ThermoCity.Weather.Application.Interfaces;

public interface IHeatIndexService
{
    double Calculate(
        double temperatureCelsius,
        double humidityPercentage);
}
