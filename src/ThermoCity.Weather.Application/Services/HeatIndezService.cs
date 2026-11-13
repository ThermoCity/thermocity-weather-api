
using ThermoCity.Weather.Application.Interfaces;

namespace ThermoCity.Weather.Application.Services;

public class HeatIndexService : IHeatIndexService
{
    public double Calculate(
        double temperatureCelsius,
        double humidityPercentage)
    {
        var temperatureFahrenheit =
            CelsiusToFahrenheit(temperatureCelsius);

        // Abaixo de 80 °F, utiliza-se a aproximação simplificada.
        if (temperatureFahrenheit < 80)
        {
            var heatIndex = CalculateSimpleHeatIndex(
                temperatureFahrenheit,
                humidityPercentage);

            return FahrenheitToCelsius(heatIndex);
        }

        var heatIndexFahrenheit = CalculateRothfusz(
            temperatureFahrenheit,
            humidityPercentage);

        heatIndexFahrenheit = ApplyHumidityAdjustment(
            temperatureFahrenheit,
            humidityPercentage,
            heatIndexFahrenheit);

        return FahrenheitToCelsius(heatIndexFahrenheit);
    }

    private static double CelsiusToFahrenheit(double temperature)
    {
        return (temperature * 9 / 5) + 32;
    }

    private static double FahrenheitToCelsius(double temperature)
    {
        return (temperature - 32) * 5 / 9;
    }

    private static double CalculateSimpleHeatIndex(
        double temperature,
        double humidity)
    {
        return 0.5 * (
            temperature +
            61.0 +
            ((temperature - 68.0) * 1.2) +
            (humidity * 0.094));
    }

    private static double CalculateRothfusz(
        double temperature,
        double humidity)
    {
        return -42.379 +
               (2.04901523 * temperature) +
               (10.14333127 * humidity) -
               (0.22475541 * temperature * humidity) -
               (0.00683783 * temperature * temperature) -
               (0.05481717 * humidity * humidity) +
               (0.00122874 * temperature * temperature * humidity) +
               (0.00085282 * temperature * humidity * humidity) -
               (0.00000199 * temperature * temperature * humidity * humidity);
    }

    private static double ApplyHumidityAdjustment(
        double temperature,
        double humidity,
        double heatIndex)
    {
        // Ajuste para umidade relativa abaixo de 13%.
        if (humidity < 13 &&
            temperature >= 80 &&
            temperature <= 112)
        {
            var adjustment = ((13 - humidity) / 4) *
                Math.Sqrt((17 - Math.Abs(temperature - 95)) / 17);

            heatIndex -= adjustment;
        }

        // Ajuste para umidade relativa acima de 85%.
        if (humidity > 85 &&
            temperature >= 80 &&
            temperature <= 87)
        {
            var adjustment = ((humidity - 85) / 10) *
                ((87 - temperature) / 5);

            heatIndex += adjustment;
        }

        return heatIndex;
    }
}
