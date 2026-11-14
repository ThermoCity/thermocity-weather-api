
using Microsoft.AspNetCore.Mvc;
using Moq;
using ThermoCity.Weather.Api.Controllers;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using Xunit;

namespace ThermoCity.Weather.UnitTests.Controllers;

public class ReadingsControllerTests
{
    private readonly Mock<IWeatherReadingRepository> _repositoryMock;
    private readonly ReadingsController _controller;

    public ReadingsControllerTests()
    {
        _repositoryMock = new Mock<IWeatherReadingRepository>();
        _controller = new ReadingsController(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetLatest_ShouldReturnOk_WhenReadingExists()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var reading = new WeatherReading
        {
            Source = WeatherSource.OpenWeather,
            TemperatureCelsius = 30,
            HumidityPercentage = 70
        };

        _repositoryMock
            .Setup(x => x.GetLatestAsync(WeatherSource.OpenWeather, cts.Token))
            .ReturnsAsync(reading);

        // Act
        var result = await _controller.GetLatest(
            WeatherSource.OpenWeather,
            cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(reading, okResult.Value);

        _repositoryMock.Verify(
            x => x.GetLatestAsync(WeatherSource.OpenWeather, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetLatest_ShouldReturnNotFound_WhenReadingDoesNotExist()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        _repositoryMock
            .Setup(x => x.GetLatestAsync(WeatherSource.OpenWeather, cts.Token))
            .ReturnsAsync((WeatherReading?)null);

        // Act
        var result = await _controller.GetLatest(
            WeatherSource.OpenWeather,
            cts.Token);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);

        Assert.Equal("Nenhuma observação encontrada.", notFoundResult.Value);
    }

    [Fact]
    public async Task GetLatest_ShouldUseOpenWeatherAsDefaultSource()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var reading = new WeatherReading
        {
            Source = WeatherSource.OpenWeather
        };

        _repositoryMock
            .Setup(x => x.GetLatestAsync(WeatherSource.OpenWeather, cts.Token))
            .ReturnsAsync(reading);

        // Act
        var result = await _controller.GetLatest(
            cancellationToken: cts.Token);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);

        _repositoryMock.Verify(
            x => x.GetLatestAsync(WeatherSource.OpenWeather, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnOk_WhenPeriodIsValid()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var startDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);

        IReadOnlyCollection<WeatherReading> readings =
        [
            new()
            {
                Source = WeatherSource.OpenWeather,
                TemperatureCelsius = 30
            }
        ];

        _repositoryMock
            .Setup(x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate.UtcDateTime,
                endDate.UtcDateTime,
                cts.Token))
            .ReturnsAsync(readings);

        // Act
        var result = await _controller.GetByPeriod(
            WeatherSource.OpenWeather,
            startDate,
            endDate,
            cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(readings, okResult.Value);

        _repositoryMock.Verify(
            x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate.UtcDateTime,
                endDate.UtcDateTime,
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnBadRequest_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var startDate = new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = await _controller.GetByPeriod(
            WeatherSource.OpenWeather,
            startDate,
            endDate);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(
            "A data final deve ser posterior à data inicial.",
            badRequestResult.Value);

        _repositoryMock.Verify(
            x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnBadRequest_WhenDatesAreEqual()
    {
        // Arrange
        var date = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = await _controller.GetByPeriod(
            WeatherSource.OpenWeather,
            date,
            date);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);

        _repositoryMock.Verify(
            x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
