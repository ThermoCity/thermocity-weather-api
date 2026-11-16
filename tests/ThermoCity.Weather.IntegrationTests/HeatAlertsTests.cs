using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using ThermoCity.Weather.IntegrationTests.Fixtures;

namespace ThermoCity.Weather.IntegrationTests.Controllers;

public class HeatAlertsTests
{
    [Fact]
    public async Task Get_ShouldReturnHeatAlertsSuccessfully()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        var readingRepository = Mock.Get(
            factory.Services.GetRequiredService<IWeatherReadingRepository>());

        var forecastRepository = Mock.Get(
            factory.Services.GetRequiredService<IDailyForecastRepository>());

        var now = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

        var today = DateOnly.FromDateTime(now.DateTime);
        var tomorrow = today.AddDays(1);

        var measurementDate = TimeZoneInfo.ConvertTimeToUtc(
            today.ToDateTime(new TimeOnly(12, 0)),
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

        var reading = new WeatherReading
        {
            Source = WeatherSource.OpenWeather,
            Status = ReadingStatus.Complete,
            MeasurementDate = measurementDate,
            CollectionDate = DateTime.UtcNow,
            HeatIndexCelsius = 37.2
        };

        var forecast = new DailyForecast
        {
            Source = WeatherSource.OpenWeather,
            ForecastDate = tomorrow.ToDateTime(TimeOnly.MinValue),
            ForecastEmissionDate = DateTime.UtcNow,
            EstimatedHeatIndexCelsius = 36.4
        };

        readingRepository
            .Setup(repository => repository.GetLatestAsync(
                WeatherSource.OpenWeather,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reading);

        readingRepository
            .Setup(repository => repository.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<WeatherReading>)[reading]);

        forecastRepository
            .Setup(repository => repository.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<DailyForecast>)[forecast]);

        // Act
        var response = await client.GetAsync("/api/heat-alerts");

        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(content);

        var root = json.RootElement;
        var todayResult = root.GetProperty("hoje");

        Assert.Equal(
            today.ToString("yyyy-MM-dd"),
            todayResult.GetProperty("data").GetString());

        Assert.Equal(4, todayResult.GetProperty("nivel").GetInt32());
        Assert.Equal("Alto risco", todayResult.GetProperty("nome").GetString());
        Assert.Equal(37.2, todayResult.GetProperty("icMaxC").GetDouble());
        Assert.Equal("OBSERVADO", todayResult.GetProperty("origem").GetString());

        var upcomingDays = root.GetProperty("proximosDias");

        var upcomingDay = Assert.Single(upcomingDays.EnumerateArray());

        Assert.Equal(
            tomorrow.ToString("yyyy-MM-dd"),
            upcomingDay.GetProperty("data").GetString());

        Assert.Equal(3, upcomingDay.GetProperty("nivel").GetInt32());
        Assert.Equal("Alerta", upcomingDay.GetProperty("nome").GetString());
        Assert.Equal(36.4, upcomingDay.GetProperty("icMaxC").GetDouble());
        Assert.Equal("PREVISTO", upcomingDay.GetProperty("origem").GetString());

        Assert.Equal(
            "A707-2003-2026-v1",
            root.GetProperty("versaoLimiar").GetString());

        Assert.False(root.GetProperty("desatualizado").GetBoolean());

        readingRepository.Verify(
            repository => repository.GetLatestAsync(
                WeatherSource.OpenWeather,
                It.IsAny<CancellationToken>()),
            Times.Once);

        forecastRepository.Verify(
            repository => repository.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Get_ShouldAllowAnonymousAccess()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/heat-alerts");

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}