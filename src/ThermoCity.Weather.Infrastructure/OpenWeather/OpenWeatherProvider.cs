using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using ThermoCity.Weather.Infrastructure.OpenWeather.Models;

namespace ThermoCity.Weather.Infrastructure.OpenWeather;

public class OpenWeatherProvider : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenWeatherProvider(
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<WeatherReading> GetCurrentWeatherAsync(
        CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();

        var latitude = _configuration["OpenWeather:Latitude"];
        var longitude = _configuration["OpenWeather:Longitude"];
        var language = _configuration["OpenWeather:Language"] ?? "pt_br";

        var endpoint =
            $"weather?lat={latitude}&lon={longitude}" +
            $"&appid={apiKey}&units=metric&lang={language}";

        var response = await _httpClient.GetFromJsonAsync<OpenWeatherCurrentResponse>(
            endpoint,
            cancellationToken);

        if (response?.Main is null)
        {
            throw new InvalidOperationException(
                "A OpenWeather retornou uma resposta sem dados climáticos.");
        }

        var temperature = response.Main.Temp;
        var humidity = response.Main.Humidity;

        return new WeatherReading
        {
            Source = WeatherSource.OpenWeather,

            MeasurementDate = DateTimeOffset
                .FromUnixTimeSeconds(response.Dt)
                .UtcDateTime,

            CollectionDate = DateTime.UtcNow,

            TemperatureCelsius = temperature,
            FeelsLikeCelsius = response.Main.FeelsLike,
            HumidityPercentage = humidity,
            PressureHpa = response.Main.Pressure,
            WindSpeedMs = response.Wind?.Speed,

            WeatherDescription = response.Weather?
                .FirstOrDefault()?.Description,

            Status = temperature.HasValue && humidity.HasValue
                ? ReadingStatus.Complete
                : ReadingStatus.Partial
        };
    }

    public async Task<IReadOnlyCollection<DailyForecast>> GetDailyForecastAsync(
        int days,
        CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();

        var latitude = _configuration["OpenWeather:Latitude"];
        var longitude = _configuration["OpenWeather:Longitude"];
        var language = _configuration["OpenWeather:Language"] ?? "pt_br";

        var endpoint =
            $"forecast?lat={latitude}&lon={longitude}" +
            $"&appid={apiKey}&units=metric&lang={language}";

        var response = await _httpClient.GetFromJsonAsync<OpenWeatherForecastResponse>(
            endpoint,
            cancellationToken);

        if (response?.List is null || response.City is null)
        {
            throw new InvalidOperationException(
                "A OpenWeather retornou uma previsão inválida.");
        }

        var timezone = TimeSpan.FromSeconds(response.City.Timezone);

        var localNow = DateTimeOffset.UtcNow.ToOffset(timezone);
        var today = DateOnly.FromDateTime(localNow.DateTime);

        var forecasts = response.List
            .Where(item => item.Main is not null)
            .Select(item => new
            {
                Item = item,
                LocalDate = DateOnly.FromDateTime(
                    DateTimeOffset
                        .FromUnixTimeSeconds(item.Dt)
                        .ToOffset(timezone)
                        .DateTime)
            })
            .Where(item =>
                item.LocalDate > today &&
                item.LocalDate <= today.AddDays(days))
            .GroupBy(item => item.LocalDate);

        var emissionDate = DateTime.UtcNow;

        return forecasts.Select(group =>
        {
            var records = group.ToList();

            var hottestRecord = records
                .OrderByDescending(item => item.Item.Main!.Temp)
                .First()
                .Item;

            var temperatures = records
                .Select(item => item.Item.Main!.Temp)
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .ToList();

            var humidity = hottestRecord.Main!.Humidity;

            return new DailyForecast
            {
                Source = WeatherSource.OpenWeather,

                ForecastDate = DateTime.SpecifyKind(
                    group.Key.ToDateTime(TimeOnly.MinValue),
                    DateTimeKind.Unspecified),

                ForecastEmissionDate = emissionDate,

                MinimumTemperatureCelsius = temperatures.Min(),
                MaximumTemperatureCelsius = temperatures.Max(),

                DayTemperatureCelsius = hottestRecord.Main.Temp,
                DayFeelsLikeCelsius = hottestRecord.Main.FeelsLike,

                HumidityPercentage = humidity,
                PressureHpa = hottestRecord.Main.Pressure,
                WindSpeedMs = hottestRecord.Wind?.Speed,

                WeatherDescription = hottestRecord.Weather?
                    .FirstOrDefault()?.Description,

                Status = hottestRecord.Main.Temp.HasValue &&
                         humidity.HasValue
                    ? ReadingStatus.Complete
                    : ReadingStatus.Partial
            };
        }).ToList();
    }

    private string GetApiKey()
    {
        var apiKey = _configuration["OpenWeather:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "A chave da API OpenWeather não foi configurada.");
        }

        return apiKey;
    }
}