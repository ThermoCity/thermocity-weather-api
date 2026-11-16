
using Microsoft.Extensions.Logging;
using Moq;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Services;
using ThermoCity.Weather.Domain.Entities;
using Xunit;

namespace ThermoCity.Weather.UnitTests.Services;

public class ForecastCollectionServiceTests
{
    private readonly Mock<IWeatherProvider> _weatherProviderMock;
    private readonly Mock<IDailyForecastRepository> _forecastRepositoryMock;
    private readonly Mock<IHeatIndexService> _heatIndexServiceMock;
    private readonly Mock<ILogger<ForecastCollectionService>> _loggerMock;

    private readonly ForecastCollectionService _service;

    private const int ForecastDays = 3;

    public ForecastCollectionServiceTests()
    {
        _weatherProviderMock = new Mock<IWeatherProvider>();
        _forecastRepositoryMock = new Mock<IDailyForecastRepository>();
        _heatIndexServiceMock = new Mock<IHeatIndexService>();
        _loggerMock = new Mock<ILogger<ForecastCollectionService>>();

        _service = new ForecastCollectionService(
            _weatherProviderMock.Object,
            _forecastRepositoryMock.Object,
            _heatIndexServiceMock.Object,
            _loggerMock.Object,
            ForecastDays);
    }

    [Fact]
    public async Task CollectAsync_ShouldCalculateHeatIndexAndPersistForecasts()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var forecasts = new List<DailyForecast>
        {
            new()
            {
                DayTemperatureCelsius = 30,
                HumidityPercentage = 70
            },
            new()
            {
                DayTemperatureCelsius = 32,
                HumidityPercentage = 80
            }
        };

        _weatherProviderMock
            .Setup(x => x.GetDailyForecastAsync(ForecastDays, cts.Token))
            .ReturnsAsync(forecasts);

        _heatIndexServiceMock
            .Setup(x => x.Calculate(30, 70))
            .Returns(35.03);

        _heatIndexServiceMock
            .Setup(x => x.Calculate(32, 80))
            .Returns(42.50);

        // Act
        await _service.CollectAsync(cts.Token);

        // Assert
        Assert.Equal(35.03, forecasts[0].EstimatedHeatIndexCelsius);
        Assert.Equal(42.50, forecasts[1].EstimatedHeatIndexCelsius);

        _weatherProviderMock.Verify(
            x => x.GetDailyForecastAsync(ForecastDays, cts.Token),
            Times.Once);

        _heatIndexServiceMock.Verify(
            x => x.Calculate(30, 70),
            Times.Once);

        _heatIndexServiceMock.Verify(
            x => x.Calculate(32, 80),
            Times.Once);

        _forecastRepositoryMock.Verify(
            x => x.UpsertManyAsync(forecasts, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_ShouldSetHeatIndexToNull_WhenTemperatureIsMissing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var forecasts = new List<DailyForecast>
        {
            new()
            {
                DayTemperatureCelsius = null,
                HumidityPercentage = 70,
                EstimatedHeatIndexCelsius = 35
            }
        };

        _weatherProviderMock
            .Setup(x => x.GetDailyForecastAsync(ForecastDays, cts.Token))
            .ReturnsAsync(forecasts);

        // Act
        await _service.CollectAsync(cts.Token);

        // Assert
        Assert.Null(forecasts[0].EstimatedHeatIndexCelsius);

        _heatIndexServiceMock.Verify(
            x => x.Calculate(It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);

        _forecastRepositoryMock.Verify(
            x => x.UpsertManyAsync(forecasts, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_ShouldSetHeatIndexToNull_WhenHumidityIsMissing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var forecasts = new List<DailyForecast>
        {
            new()
            {
                DayTemperatureCelsius = 30,
                HumidityPercentage = null,
                EstimatedHeatIndexCelsius = 35
            }
        };

        _weatherProviderMock
            .Setup(x => x.GetDailyForecastAsync(ForecastDays, cts.Token))
            .ReturnsAsync(forecasts);

        // Act
        await _service.CollectAsync(cts.Token);

        // Assert
        Assert.Null(forecasts[0].EstimatedHeatIndexCelsius);

        _heatIndexServiceMock.Verify(
            x => x.Calculate(It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);

        _forecastRepositoryMock.Verify(
            x => x.UpsertManyAsync(forecasts, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_ShouldPersistEmptyCollection_WhenProviderReturnsNoForecasts()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var forecasts = new List<DailyForecast>();

        _weatherProviderMock
            .Setup(x => x.GetDailyForecastAsync(ForecastDays, cts.Token))
            .ReturnsAsync(forecasts);

        // Act
        await _service.CollectAsync(cts.Token);

        // Assert
        _heatIndexServiceMock.Verify(
            x => x.Calculate(It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);

        _forecastRepositoryMock.Verify(
            x => x.UpsertManyAsync(forecasts, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_ShouldNotPropagateException_WhenProviderFails()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        _weatherProviderMock
            .Setup(x => x.GetDailyForecastAsync(ForecastDays, cts.Token))
            .ThrowsAsync(new HttpRequestException("OpenWeather unavailable"));

        // Act
        var exception = await Record.ExceptionAsync(
            () => _service.CollectAsync(cts.Token));

        // Assert
        Assert.Null(exception);

        _forecastRepositoryMock.Verify(
            x => x.UpsertManyAsync(
                It.IsAny<IEnumerable<DailyForecast>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_ShouldNotPropagateException_WhenRepositoryFails()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var forecasts = new List<DailyForecast>
        {
            new()
            {
                DayTemperatureCelsius = 30,
                HumidityPercentage = 70
            }
        };

        _weatherProviderMock
            .Setup(x => x.GetDailyForecastAsync(ForecastDays, cts.Token))
            .ReturnsAsync(forecasts);

        _heatIndexServiceMock
            .Setup(x => x.Calculate(30, 70))
            .Returns(35.03);

        _forecastRepositoryMock
            .Setup(x => x.UpsertManyAsync(forecasts, cts.Token))
            .ThrowsAsync(new Exception("MongoDB unavailable"));

        // Act
        var exception = await Record.ExceptionAsync(
            () => _service.CollectAsync(cts.Token));

        // Assert
        Assert.Null(exception);

        _forecastRepositoryMock.Verify(
            x => x.UpsertManyAsync(forecasts, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_ShouldPropagateCancellation_WhenTokenIsCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _weatherProviderMock
            .Setup(x => x.GetDailyForecastAsync(ForecastDays, cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.CollectAsync(cts.Token));

        _forecastRepositoryMock.Verify(
            x => x.UpsertManyAsync(
                It.IsAny<IEnumerable<DailyForecast>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
