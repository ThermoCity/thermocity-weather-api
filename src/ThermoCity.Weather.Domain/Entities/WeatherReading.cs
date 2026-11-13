using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Domain.Entities;

public class WeatherReading
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonRepresentation(BsonType.String)]
    public WeatherSource Source { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ReadingStatus Status { get; set; }

    // Momento da medição informado pela fonte.
    public DateTime MeasurementDate { get; set; }

    // Momento em que o Worker realizou a coleta.
    public DateTime CollectionDate { get; set; }

    // Dados meteorológicos observados.
    public double? TemperatureCelsius { get; set; }

    public double? FeelsLikeCelsius { get; set; }

    public double? HumidityPercentage { get; set; }

    public double? PressureHpa { get; set; }

    public double? WindSpeedMs { get; set; }

    public string? WeatherDescription { get; set; }

    // Calculado pelo ThermoCity utilizando a fórmula de Rothfusz.
    public double? HeatIndexCelsius { get; set; }
}