using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Domain.Entities;

public class DailyForecast
{
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [BsonRepresentation(BsonType.String)]
    public WeatherSource Source { get; set; }

    // Dia ao qual a previsão se refere.
    public DateTime ForecastDate { get; set; }

    // Momento em que a previsão foi emitida pela fonte.
    public DateTime ForecastEmissionDate { get; set; }

    // Temperaturas previstas.
    public double? MinimumTemperatureCelsius { get; set; }

    public double? MaximumTemperatureCelsius { get; set; }

    // Temperatura prevista para o período diurno.
    public double? DayTemperatureCelsius { get; set; }

    // Sensação térmica prevista para o período diurno.
    public double? DayFeelsLikeCelsius { get; set; }

    // Umidade relativa do ar prevista.
    public double? HumidityPercentage { get; set; }

    public double? PressureHpa { get; set; }

    public double? WindSpeedMs { get; set; }

    public string? WeatherDescription { get; set; }

    // Índice de calor estimado, quando houver dados adequados.
    public double? EstimatedHeatIndexCelsius { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ReadingStatus Status { get; set; }
}