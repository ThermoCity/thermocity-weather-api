using System.Text.Json.Serialization;

namespace ThermoCity.Weather.Infrastructure.OpenWeather.Models;

public class OpenWeatherCurrentResponse
{
    [JsonPropertyName("dt")]
    public long Dt { get; set; }

    [JsonPropertyName("main")]
    public OpenWeatherMain? Main { get; set; }

    [JsonPropertyName("wind")]
    public OpenWeatherWind? Wind { get; set; }

    [JsonPropertyName("weather")]
    public List<OpenWeatherWeather>? Weather { get; set; }
}

public class OpenWeatherMain
{
    [JsonPropertyName("temp")]
    public double? Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public double? FeelsLike { get; set; }

    [JsonPropertyName("humidity")]
    public double? Humidity { get; set; }

    [JsonPropertyName("pressure")]
    public double? Pressure { get; set; }
}

public class OpenWeatherWind
{
    [JsonPropertyName("speed")]
    public double? Speed { get; set; }
}

public class OpenWeatherWeather
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}