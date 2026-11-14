
using Microsoft.Extensions.Logging;
using Moq;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Services;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using Xunit;

namespace ThermoCity.Weather.UnitTests.Services;

public class DailyConsolidationServiceTests
{
    private readonly Mock<IWeatherReadingRepository> _readingRepositoryMock;
    private readonly Mock<IDailyWeatherSummaryRepository> _summaryRepositoryMock;
    private readonly Mock<ILogger<DailyConsolidationService>> _loggerMock;

    private readonly DailyConsolidationService _service;

    public DailyConsolidationServiceTests()
    {
        _readingRepositoryMock = new Mock<IWeatherReadingRepository>();
        _summaryRepositoryMock = new Mock<IDailyWeatherSummaryRepository>();
        _loggerMock = new Mock<ILogger<DailyConsolidationService>>();

        _service = new DailyConsolidationService(
            _readingRepositoryMock.Object,
            _summaryRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ConsolidatePreviousDayAsync_ShouldCalculateAndPersistSummary()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var readings = new List<WeatherReading>
        {
            new()
            {
                Status = ReadingStatus.Complete,
                TemperatureCelsius = 25,
                HumidityPercentage = 60,
                HeatIndexCelsius = 25.5
            },
            new()
            {
                Status = ReadingStatus.Complete,
                TemperatureCelsius = 30,
                HumidityPercentage = 80,
                HeatIndexCelsius = 35.0
            },
            new()
            {
                Status = ReadingStatus.Complete,
                TemperatureCelsius = 27,
                HumidityPercentage = 70,
                HeatIndexCelsius = 29.0
            }
        };

        _readingRepositoryMock
            .Setup(x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                cts.Token))
            .ReturnsAsync(readings);

        // Act
        await _service.ConsolidatePreviousDayAsync(cts.Token);

        // Assert
        _summaryRepositoryMock.Verify(
            x => x.UpsertAsync(
                It.Is<DailyWeatherSummary>(summary =>
                    summary.MinimumTemperatureCelsius == 25 &&
                    summary.AverageTemperatureCelsius == 27.333333333333332 &&
                    summary.MaximumTemperatureCelsius == 30 &&
                    summary.MinimumHumidityPercentage == 60 &&
                    summary.AverageHumidityPercentage == 70 &&
                    summary.MaximumHumidityPercentage == 80 &&
                    summary.MinimumHeatIndexCelsius == 25.5 &&
                    summary.AverageHeatIndexCelsius == 29.833333333333332 &&
                    summary.MaximumHeatIndexCelsius == 35.0 &&
                    summary.ObservationCount == 3),
                cts.Token),
            Times.Exactly(Enum.GetValues<WeatherSource>().Length));
    }

    [Fact]
    public async Task ConsolidatePreviousDayAsync_ShouldIgnoreIncompleteReadings()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var readings = new List<WeatherReading>
        {
            new()
            {
                Status = ReadingStatus.Complete,
                TemperatureCelsius = 28,
                HumidityPercentage = 65,
                HeatIndexCelsius = 29
            },
            new()
            {
                Status = ReadingStatus.Partial,
                TemperatureCelsius = 40,
                HumidityPercentage = 90,
                HeatIndexCelsius = 50
            }
        };

        _readingRepositoryMock
            .Setup(x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                cts.Token))
            .ReturnsAsync(readings);

        // Act
        await _service.ConsolidatePreviousDayAsync(cts.Token);

        // Assert
        _summaryRepositoryMock.Verify(
            x => x.UpsertAsync(
                It.Is<DailyWeatherSummary>(summary =>
                    summary.ObservationCount == 1 &&
                    summary.MinimumTemperatureCelsius == 28 &&
                    summary.MaximumTemperatureCelsius == 28),
                cts.Token),
            Times.Exactly(Enum.GetValues<WeatherSource>().Length));
    }

    [Fact]
    public async Task ConsolidatePreviousDayAsync_ShouldSetHeatIndexMetricsToNull_WhenNoHeatIndexExists()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var readings = new List<WeatherReading>
        {
            new()
            {
                Status = ReadingStatus.Complete,
                TemperatureCelsius = 28,
                HumidityPercentage = 65,
                HeatIndexCelsius = null
            }
        };

        _readingRepositoryMock
            .Setup(x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                cts.Token))
            .ReturnsAsync(readings);

        // Act
        await _service.ConsolidatePreviousDayAsync(cts.Token);

        // Assert
        _summaryRepositoryMock.Verify(
            x => x.UpsertAsync(
                It.Is<DailyWeatherSummary>(summary =>
                    summary.MinimumHeatIndexCelsius == null &&
                    summary.AverageHeatIndexCelsius == null &&
                    summary.MaximumHeatIndexCelsius == null),
                cts.Token),
            Times.Exactly(Enum.GetValues<WeatherSource>().Length));
    }

    [Fact]
    public async Task ConsolidatePreviousDayAsync_ShouldNotPersistSummary_WhenNoValidReadingsExist()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        var readings = new List<WeatherReading>
        {
            new()
            {
                Status = ReadingStatus.Partial,
                TemperatureCelsius = 30,
                HumidityPercentage = 70
            }
        };

        _readingRepositoryMock
            .Setup(x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                cts.Token))
            .ReturnsAsync(readings);

        // Act
        await _service.ConsolidatePreviousDayAsync(cts.Token);

        // Assert
        _summaryRepositoryMock.Verify(
            x => x.UpsertAsync(
                It.IsAny<DailyWeatherSummary>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConsolidatePreviousDayAsync_ShouldUsePreviousLocalDayBoundaries()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        DateTime capturedStart = default;
        DateTime capturedEnd = default;

        _readingRepositoryMock
            .Setup(x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                cts.Token))
            .Callback<WeatherSource, DateTime, DateTime, CancellationToken>(
                (_, start, end, _) =>
                {
                    capturedStart = start;
                    capturedEnd = end;
                })
            .ReturnsAsync(new List<WeatherReading>());

        // Act
        await _service.ConsolidatePreviousDayAsync(cts.Token);

        // Assert
        var localToday = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).Date;

        var expectedReferenceDate = localToday.AddDays(-1);

        var expectedStart = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(expectedReferenceDate, DateTimeKind.Unspecified),
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

        var expectedEnd = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(expectedReferenceDate.AddDays(1), DateTimeKind.Unspecified),
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));

        Assert.Equal(expectedStart, capturedStart);
        Assert.Equal(expectedEnd, capturedEnd);
    }

    [Fact]
    public async Task ConsolidatePreviousDayAsync_ShouldPropagateCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _readingRepositoryMock
            .Setup(x => x.GetByPeriodAsync(
                It.IsAny<WeatherSource>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.ConsolidatePreviousDayAsync(cts.Token));

        _summaryRepositoryMock.Verify(
            x => x.UpsertAsync(
                It.IsAny<DailyWeatherSummary>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
