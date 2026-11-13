using System.Text.Json.Serialization;

namespace ThermoCity.Weather.Infrastructure.OpenWeather.Models;

public class OpenWeatherForecastResponse
{
    [JsonPropertyName("list")]
    public List<OpenWeatherForecastItem>? List { get; set; }

    [JsonPropertyName("city")]
    public OpenWeatherCity? City { get; set; }
}

public class OpenWeatherForecastItem
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

public class OpenWeatherCity
{
    [JsonPropertyName("timezone")]
    public int Timezone { get; set; }
}