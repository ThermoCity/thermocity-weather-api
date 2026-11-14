
using Microsoft.AspNetCore.Mvc;
using Moq;
using ThermoCity.Weather.Api.Controllers;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using Xunit;

namespace ThermoCity.Weather.UnitTests.Controllers;

public class DailySummariesControllerTests
{
    private readonly Mock<IDailyWeatherSummaryRepository> _repositoryMock;
    private readonly DailySummariesController _controller;

    public DailySummariesControllerTests()
    {
        _repositoryMock = new Mock<IDailyWeatherSummaryRepository>();
        _controller = new DailySummariesController(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetByDate_ShouldReturnOk_WhenSummaryExists()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var date = new DateOnly(2026, 11, 10);
        var summary = new DailyWeatherSummary
        {
            Source = WeatherSource.OpenWeather,
            ReferenceDate = date.ToDateTime(TimeOnly.MinValue),
            ObservationCount = 288
        };

        _repositoryMock
            .Setup(x => x.GetByDateAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                cts.Token))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetByDate(
            date,
            WeatherSource.OpenWeather,
            cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(summary, okResult.Value);

        _repositoryMock.Verify(
            x => x.GetByDateAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetByDate_ShouldReturnNotFound_WhenSummaryDoesNotExist()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var date = new DateOnly(2026, 11, 10);

        _repositoryMock
            .Setup(x => x.GetByDateAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                cts.Token))
            .ReturnsAsync((DailyWeatherSummary?)null);

        // Act
        var result = await _controller.GetByDate(
            date,
            WeatherSource.OpenWeather,
            cts.Token);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);

        Assert.Equal(
            "Nenhum resumo encontrado para essa data.",
            notFoundResult.Value);
    }

    [Fact]
    public async Task GetByDate_ShouldUseOpenWeatherAsDefaultSource()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var date = new DateOnly(2026, 11, 10);

        _repositoryMock
            .Setup(x => x.GetByDateAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                cts.Token))
            .ReturnsAsync(new DailyWeatherSummary());

        // Act
        var result = await _controller.GetByDate(
            date,
            cancellationToken: cts.Token);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);

        _repositoryMock.Verify(
            x => x.GetByDateAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnOk_WhenPeriodIsValid()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var startDate = new DateOnly(2026, 11, 1);
        var endDate = new DateOnly(2026, 11, 10);

        IReadOnlyCollection<DailyWeatherSummary> summaries =
        [
            new()
            {
                Source = WeatherSource.OpenWeather,
                ReferenceDate = startDate.ToDateTime(TimeOnly.MinValue)
            },
            new()
            {
                Source = WeatherSource.OpenWeather,
                ReferenceDate = endDate.ToDateTime(TimeOnly.MinValue)
            }
        ];

        _repositoryMock
            .Setup(x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                startDate.ToDateTime(TimeOnly.MinValue),
                endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                cts.Token))
            .ReturnsAsync(summaries);

        // Act
        var result = await _controller.GetByPeriod(
            WeatherSource.OpenWeather,
            startDate,
            endDate,
            cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(summaries, okResult.Value);

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

        IReadOnlyCollection<DailyWeatherSummary> summaries =
        [
            new()
            {
                Source = WeatherSource.OpenWeather,
                ReferenceDate = date.ToDateTime(TimeOnly.MinValue)
            }
        ];

        _repositoryMock
            .Setup(x => x.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                date.ToDateTime(TimeOnly.MinValue),
                date.AddDays(1).ToDateTime(TimeOnly.MinValue),
                cts.Token))
            .ReturnsAsync(summaries);

        // Act
        var result = await _controller.GetByPeriod(
            WeatherSource.OpenWeather,
            date,
            date,
            cts.Token);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(summaries, okResult.Value);
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnBadRequest_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var startDate = new DateOnly(2026, 11, 10);
        var endDate = new DateOnly(2026, 11, 1);

        // Act
        var result = await _controller.GetByPeriod(
            WeatherSource.OpenWeather,
            startDate,
            endDate);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(
            "A data final deve ser igual ou posterior à inicial.",
            badRequestResult.Value);

        _repositoryMock.Verify(
            x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
