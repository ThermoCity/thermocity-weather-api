using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Domain.Entities;

public class DailyWeatherSummary
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public WeatherSource Source { get; set; }

    // Dia ao qual o resumo pertence.
    public DateTime ReferenceDate { get; set; }

    // Temperaturas.
    public double? MinimumTemperatureCelsius { get; set; }

    public double? AverageTemperatureCelsius { get; set; }

    public double? MaximumTemperatureCelsius { get; set; }

    // Umidade.
    public double? MinimumHumidityPercentage { get; set; }

    public double? AverageHumidityPercentage { get; set; }

    public double? MaximumHumidityPercentage { get; set; }

    // Índice de calor.
    public double? MinimumHeatIndexCelsius { get; set; }

    public double? AverageHeatIndexCelsius { get; set; }

    public double? MaximumHeatIndexCelsius { get; set; }

    // Quantidade de observações válidas utilizadas.
    public int ObservationCount { get; set; }

    // Momento em que a consolidação foi realizada.
    public DateTime ConsolidationDate { get; set; }
}