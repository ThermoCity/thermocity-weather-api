
using Microsoft.Extensions.Logging;
using Moq;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Services;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using Xunit;

namespace ThermoCity.Weather.UnitTests.Services;

public class WeatherCollectionServiceTests
{
    private readonly Mock<IWeatherProvider> _weatherProviderMock;
    private readonly Mock<IWeatherReadingRepository> _readingRepositoryMock;
    private readonly Mock<IHeatIndexService> _heatIndexServiceMock;
    private readonly Mock<ILogger<WeatherCollectionService>> _loggerMock;

    private readonly WeatherCollectionService _service;

    public WeatherCollectionServiceTests()
    {
        _weatherProviderMock = new Mock<IWeatherProvider>();
        _readingRepositoryMock = new Mock<IWeatherReadingRepository>();
        _heatIndexServiceMock = new Mock<IHeatIndexService>();
        _loggerMock = new Mock<ILogger<WeatherCollectionService>>();

        _service = new WeatherCollectionService(
            _weatherProviderMock.Object,
            _readingRepositoryMock.Object,
            _heatIndexServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CollectAsync_ShouldCalculateHeatIndexAndPersistReading()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var reading = new WeatherReading
        {
            TemperatureCelsius = 30,
            HumidityPercentage = 70
        };

        _weatherProviderMock
            .Setup(x => x.GetCurrentWeatherAsync(cts.Token))
            .ReturnsAsync(reading);

        _heatIndexServiceMock
            .Setup(x => x.Calculate(30, 70))
            .Returns(35.03);

        // Act
        await _service.CollectAsync(cts.Token);

        // Assert
        Assert.Equal(ReadingStatus.Complete, reading.Status);
        Assert.Equal(35.03, reading.HeatIndexCelsius);

        _heatIndexServiceMock.Verify(
            x => x.Calculate(30, 70),
            Times.Once);

        _readingRepositoryMock.Verify(
            x => x.UpsertAsync(reading, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_ShouldPersistPartialReading_WhenTemperatureIsMissing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var reading = new WeatherReading
        {
            TemperatureCelsius = null,
            HumidityPercentage = 70,
            HeatIndexCelsius = 30
        };

        _weatherProviderMock
            .Setup(x => x.GetCurrentWeatherAsync(cts.Token))
            .ReturnsAsync(reading);

        // Act
        await _service.CollectAsync(cts.Token);

        // Assert
        Assert.Equal(ReadingStatus.Partial, reading.Status);
        Assert.Null(reading.HeatIndexCelsius);

        _heatIndexServiceMock.Verify(
            x => x.Calculate(It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);

        _readingRepositoryMock.Verify(
            x => x.UpsertAsync(reading, cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task CollectAsync_ShouldPersistPartialReading_WhenHumidityIsMissing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var reading = new WeatherReading
        {
            TemperatureCelsius = 30,
            HumidityPercentage = null,
            HeatIndexCelsius = 35
        };

        _weatherProviderMock
            .Setup(x => x.GetCurrentWeatherAsync(cts.Token))
            .ReturnsAsync(reading);

        // Act
        await _service.CollectAsync(cts.Token);

        // Assert
        Assert.Equal(ReadingStatus.Partial, reading.Status);
        Assert.Null(reading.HeatIndexCelsius);

        _heatIndexServiceMock.Verify(
            x => x.Calculate(It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);

        _readingRepositoryMock.Verify(
            x => x.UpsertAsync(reading, cts.Token),
            Times.Once);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(61)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task CollectAsync_ShouldDiscardReading_WhenTemperatureIsInvalid(
        double temperature)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var reading = new WeatherReading
        {
            TemperatureCelsius = temperature,
            HumidityPercentage = 70
        };

        _weatherProviderMock
            .Setup(x => x.GetCurrentWeatherAsync(cts.Token))
            .ReturnsAsync(reading);

        // Act
        await _service.CollectAsync(cts.Token);

        // Assert
        _readingRepositoryMock.Verify(
            x => x.UpsertAsync(
                It.IsAny<WeatherReading>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _heatIndexServiceMock.Verify(
            x => x.Calculate(It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task CollectAsync_ShouldDiscardReading_WhenHumidityIsInvalid(
        double humidity)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var reading = new WeatherReading
        {
            TemperatureCelsius = 30,
            HumidityPercentage = humidity
        };

        _weatherProviderMock
            .Setup(x => x.GetCurrentWeatherAsync(cts.Token))
            .ReturnsAsync(reading);

        // Act
        await _service.CollectAsync(cts.Token);

        // Assert
        _readingRepositoryMock.Verify(
            x => x.UpsertAsync(
                It.IsAny<WeatherReading>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _heatIndexServiceMock.Verify(
            x => x.Calculate(It.IsAny<double>(), It.IsAny<double>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_ShouldNotPropagateException_WhenProviderFails()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        _weatherProviderMock
            .Setup(x => x.GetCurrentWeatherAsync(cts.Token))
            .ThrowsAsync(new HttpRequestException("OpenWeather unavailable"));

        // Act
        var exception = await Record.ExceptionAsync(
            () => _service.CollectAsync(cts.Token));

        // Assert
        Assert.Null(exception);

        _readingRepositoryMock.Verify(
            x => x.UpsertAsync(
                It.IsAny<WeatherReading>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CollectAsync_ShouldPropagateCancellation_WhenTokenIsCancelled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _weatherProviderMock
            .Setup(x => x.GetCurrentWeatherAsync(cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.CollectAsync(cts.Token));

        // Assert
        _readingRepositoryMock.Verify(
            x => x.UpsertAsync(
                It.IsAny<WeatherReading>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
