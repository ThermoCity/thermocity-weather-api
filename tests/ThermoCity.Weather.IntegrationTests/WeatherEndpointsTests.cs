
using System.Net;
using System.Text.Json;
using Moq;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using ThermoCity.Weather.IntegrationTests.Fixtures;
using Xunit;

namespace ThermoCity.Weather.IntegrationTests;

public class WeatherEndpointsTests
{
    [Fact]
    public async Task GetLatest_ShouldReturnOk_WhenReadingExists()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        factory.WeatherReadingRepositoryMock
            .Setup(x => x.GetLatestAsync(
                WeatherSource.OpenWeather,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeatherReading
            {
                Source = WeatherSource.OpenWeather,
                TemperatureCelsius = 30,
                HumidityPercentage = 70
            });

        // Act
        var response = await client.GetAsync(
            "/api/readings/latest?source=OpenWeather");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("temperatureCelsius", content);

        factory.WeatherReadingRepositoryMock.Verify(
            x => x.GetLatestAsync(
                WeatherSource.OpenWeather,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLatest_ShouldReturnNotFound_WhenReadingDoesNotExist()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        factory.WeatherReadingRepositoryMock
            .Setup(x => x.GetLatestAsync(
                WeatherSource.OpenWeather,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherReading?)null);

        // Act
        var response = await client.GetAsync(
            "/api/readings/latest?source=OpenWeather");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReadingsByPeriod_ShouldReturnOk_WhenPeriodIsValid()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        var startDate = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);

        IReadOnlyCollection<WeatherReading> readings =
        [
            new()
            {
                Source = WeatherSource.OpenWeather,
                TemperatureCelsius = 30
            }
        ];

        factory.WeatherReadingRepositoryMock
            .Setup(x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate,
                endDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Act
        var response = await client.GetAsync(
            "/api/readings?source=OpenWeather" +
            "&startDate=2026-11-10T00:00:00Z" +
            "&endDate=2026-11-11T00:00:00Z");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        factory.WeatherReadingRepositoryMock.Verify(
            x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate,
                endDate,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetReadingsByPeriod_ShouldReturnBadRequest_WhenPeriodIsInvalid()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/readings?source=OpenWeather" +
            "&startDate=2026-11-11T00:00:00Z" +
            "&endDate=2026-11-10T00:00:00Z");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        factory.WeatherReadingRepositoryMock.Verify(
            x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetForecastsByPeriod_ShouldReturnOk_WhenPeriodIsValid()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        var startDate = new DateTime(2026, 11, 10);
        var endDate = new DateTime(2026, 11, 12);

        IReadOnlyCollection<DailyForecast> forecasts =
        [
            new()
            {
                Source = WeatherSource.OpenWeather,
                ForecastDate = startDate
            }
        ];

        factory.DailyForecastRepositoryMock
            .Setup(x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate,
                endDate.AddDays(1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(forecasts);

        // Act
        var response = await client.GetAsync(
            "/api/forecasts?source=OpenWeather" +
            "&startDate=2026-11-10" +
            "&endDate=2026-11-12");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        factory.DailyForecastRepositoryMock.Verify(
            x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate,
                endDate.AddDays(1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetForecastsByPeriod_ShouldReturnBadRequest_WhenPeriodIsInvalid()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/forecasts?source=OpenWeather" +
            "&startDate=2026-11-12" +
            "&endDate=2026-11-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        factory.DailyForecastRepositoryMock.Verify(
            x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDailySummary_ShouldReturnOk_WhenSummaryExists()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        var date = new DateOnly(2026, 11, 10);

        factory.DailyWeatherSummaryRepositoryMock
            .Setup(x => x.GetByDateAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DailyWeatherSummary
            {
                Source = WeatherSource.OpenWeather,
                ReferenceDate = date.ToDateTime(TimeOnly.MinValue),
                ObservationCount = 288
            });

        // Act
        var response = await client.GetAsync(
            "/api/daily-summaries/2026-11-10?source=OpenWeather");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("observationCount", content);
    }

    [Fact]
    public async Task GetDailySummary_ShouldReturnNotFound_WhenSummaryDoesNotExist()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        var date = new DateOnly(2026, 11, 10);

        factory.DailyWeatherSummaryRepositoryMock
            .Setup(x => x.GetByDateAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DailyWeatherSummary?)null);

        // Act
        var response = await client.GetAsync(
            "/api/daily-summaries/2026-11-10?source=OpenWeather");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDailySummariesByPeriod_ShouldReturnOk_WhenPeriodIsValid()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        var startDate = new DateOnly(2026, 11, 10);
        var endDate = new DateOnly(2026, 11, 12);

        factory.DailyWeatherSummaryRepositoryMock
            .Setup(x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate.ToDateTime(TimeOnly.MinValue),
                endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyWeatherSummary>());

        // Act
        var response = await client.GetAsync(
            "/api/daily-summaries?source=OpenWeather" +
            "&startDate=2026-11-10" +
            "&endDate=2026-11-12");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        factory.DailyWeatherSummaryRepositoryMock.Verify(
            x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate.ToDateTime(TimeOnly.MinValue),
                endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDailySummariesByPeriod_ShouldReturnBadRequest_WhenPeriodIsInvalid()
    {
        // Arrange
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(
            "/api/daily-summaries?source=OpenWeather" +
            "&startDate=2026-11-12" +
            "&endDate=2026-11-10");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        factory.DailyWeatherSummaryRepositoryMock.Verify(
            x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
