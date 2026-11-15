
using Microsoft.AspNetCore.Mvc;
using Moq;
using ThermoCity.Weather.Api.Controllers;
using ThermoCity.Weather.Application.Exceptions;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using Xunit;

namespace ThermoCity.Weather.UnitTests.Controllers;

public class ForecastsControllerTests
{
    private readonly Mock<IDailyForecastRepository> _repositoryMock;
    private readonly ForecastsController _controller;

    public ForecastsControllerTests()
    {
        _repositoryMock = new Mock<IDailyForecastRepository>();
        _controller = new ForecastsController(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnOk_WhenPeriodIsValid()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var startDate = new DateOnly(2026, 11, 1);
        var endDate = new DateOnly(2026, 11, 10);

        IReadOnlyCollection<DailyForecast> forecasts =
        [
            new()
            {
                Source = WeatherSource.OpenWeather,
                ForecastDate = startDate.ToDateTime(TimeOnly.MinValue)
            },
            new()
            {
                Source = WeatherSource.OpenWeather,
                ForecastDate = endDate.ToDateTime(TimeOnly.MinValue)
            }
        ];

        _repositoryMock
            .Setup(x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate.ToDateTime(TimeOnly.MinValue),
                endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                cts.Token))
            .ReturnsAsync(forecasts);

        // Act
        var result = await _controller.GetByPeriod(
            WeatherSource.OpenWeather,
            startDate,
            endDate,
            cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(forecasts, okResult.Value);

        _repositoryMock.Verify(
            x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate.ToDateTime(TimeOnly.MinValue),
                endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnOk_WhenStartDateEqualsEndDate()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var date = new DateOnly(2026, 11, 10);

        IReadOnlyCollection<DailyForecast> forecasts =
        [
            new()
            {
                Source = WeatherSource.OpenWeather,
                ForecastDate = date.ToDateTime(TimeOnly.MinValue)
            }
        ];

        _repositoryMock
            .Setup(x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                date.AddDays(1).ToDateTime(TimeOnly.MinValue),
                cts.Token))
            .ReturnsAsync(forecasts);

        // Act
        var result = await _controller.GetByPeriod(
            WeatherSource.OpenWeather,
            date,
            date,
            cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(forecasts, okResult.Value);

        _repositoryMock.Verify(
            x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                date.AddDays(1).ToDateTime(TimeOnly.MinValue),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetByPeriod_ShouldThrowInvalidInputException_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var startDate = new DateOnly(2026, 11, 10);
        var endDate = new DateOnly(2026, 11, 1);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidInputException>(() => _controller.GetByPeriod(
            WeatherSource.OpenWeather,
            startDate,
            endDate));

        Assert.Equal(
            "A data final deve ser igual ou posterior à inicial.",
            ex.Message);

        _repositoryMock.Verify(
            x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnEmptyCollection_WhenNoForecastsExist()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var startDate = new DateOnly(2026, 11, 1);
        var endDate = new DateOnly(2026, 11, 10);

        IReadOnlyCollection<DailyForecast> forecasts = [];

        _repositoryMock
            .Setup(x => x.GetByPeriodAsync(
                WeatherSource.INMET,
                startDate.ToDateTime(TimeOnly.MinValue),
                endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                cts.Token))
            .ReturnsAsync(forecasts);

        // Act
        var result = await _controller.GetByPeriod(
            WeatherSource.INMET,
            startDate,
            endDate,
            cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(forecasts, okResult.Value);
    }
}
