using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Services;
using ThermoCity.Weather.Domain;
using Moq;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.UnitTests.Services;

public class HeatAlertServiceTests
{
    private readonly Mock<IWeatherReadingRepository> _readingRepository;
    private readonly Mock<IDailyForecastRepository> _forecastRepository;
    private readonly HeatAlertService _service;

    private static readonly TimeZoneInfo BrazilTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public HeatAlertServiceTests()
    {
        _readingRepository = new Mock<IWeatherReadingRepository>();
        _forecastRepository = new Mock<IDailyForecastRepository>();

        _readingRepository
            .Setup(repository => repository.GetLatestAsync(
                WeatherSource.OpenWeather,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherReading?)null);

        _readingRepository
            .Setup(repository => repository.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<WeatherReading>)[]);

        _forecastRepository
            .Setup(repository => repository.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<DailyForecast>)[]);

        _service = new HeatAlertService(
            _readingRepository.Object,
            _forecastRepository.Object);
    }

    [Theory]
    [InlineData(34.9, 1, "Normal")]
    [InlineData(35.0, 2, "Atenção")]
    [InlineData(36.0, 3, "Alerta")]
    [InlineData(37.0, 4, "Alto risco")]
    [InlineData(38.5, 5, "Emergência")]
    public async Task GetHeatAlertsAsync_ShouldClassifyObservedHeatIndex(
        double heatIndex,
        int expectedLevel,
        string expectedName)
    {
        // Arrange
        var reading = CreateReading(Today(), heatIndex);

        SetupReadings([reading]);

        // Act
        var result = await _service.GetHeatAlertsAsync(
            CancellationToken.None);

        // Assert
        Assert.NotNull(result.Today);
        Assert.Equal(expectedLevel, result.Today.Level);
        Assert.Equal(expectedName, result.Today.Name);
        Assert.Equal("OBSERVADO", result.Today.Origin);
        Assert.Equal(heatIndex, result.Today.MaximumHeatIndexCelsius);
    }

    [Theory]
    [InlineData(35.0, 3)]
    [InlineData(36.0, 4)]
    [InlineData(37.0, 5)]
    public async Task GetHeatAlertsAsync_ShouldIncreaseLevelAfterThreeConsecutiveDays(
        double heatIndex,
        int expectedLevel)
    {
        // Arrange
        var readings = new[]
        {
            CreateReading(Today().AddDays(-2), heatIndex),
            CreateReading(Today().AddDays(-1), heatIndex),
            CreateReading(Today(), heatIndex)
        };

        SetupReadings(readings);

        // Act
        var result = await _service.GetHeatAlertsAsync(
            CancellationToken.None);

        // Assert
        Assert.NotNull(result.Today);
        Assert.Equal(expectedLevel, result.Today.Level);
    }

    [Fact]
    public async Task GetHeatAlertsAsync_ShouldUseForecastForUpcomingDays()
    {
        // Arrange
        var tomorrow = Today().AddDays(1);

        var forecast = CreateForecast(tomorrow, 37.2);

        _forecastRepository
            .Setup(repository => repository.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<DailyForecast>)[forecast]);

        // Act
        var result = await _service.GetHeatAlertsAsync(
            CancellationToken.None);

        // Assert
        var upcomingDay = Assert.Single(result.UpcomingDays);

        Assert.Equal(tomorrow, upcomingDay.Date);
        Assert.Equal(4, upcomingDay.Level);
        Assert.Equal("Alto risco", upcomingDay.Name);
        Assert.Equal("PREVISTO", upcomingDay.Origin);
        Assert.Equal(37.2, upcomingDay.MaximumHeatIndexCelsius);
    }

    [Fact]
    public async Task GetHeatAlertsAsync_ShouldReturnNoDataWhenThereAreNoReadings()
    {
        // Act
        var result = await _service.GetHeatAlertsAsync(
            CancellationToken.None);

        // Assert
        Assert.Null(result.Today);
        Assert.Empty(result.UpcomingDays);
        Assert.Equal("Sem dados meteorológicos disponíveis", result.Criterion);
        Assert.Equal("Aguardando leitura meteorológica", result.ExpectedAction);
        Assert.True(result.IsStale);
    }

    [Fact]
    public async Task GetHeatAlertsAsync_ShouldUseLatestForecastEmission()
    {
        // Arrange
        var tomorrow = Today().AddDays(1);

        var olderForecast = CreateForecast(
            tomorrow,
            35.0,
            DateTime.UtcNow.AddHours(-12));

        var latestForecast = CreateForecast(
            tomorrow,
            37.5,
            DateTime.UtcNow);

        _forecastRepository
            .Setup(repository => repository.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<DailyForecast>)
                [olderForecast, latestForecast]);

        // Act
        var result = await _service.GetHeatAlertsAsync(
            CancellationToken.None);

        // Assert
        var upcomingDay = Assert.Single(result.UpcomingDays);

        Assert.Equal(37.5, upcomingDay.MaximumHeatIndexCelsius);
        Assert.Equal(4, upcomingDay.Level);
    }

    private void SetupReadings(IReadOnlyCollection<WeatherReading> readings)
    {
        _readingRepository
            .Setup(repository => repository.GetLatestAsync(
                WeatherSource.OpenWeather,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings
                .OrderByDescending(reading => reading.CollectionDate)
                .FirstOrDefault());

        _readingRepository
            .Setup(repository => repository.GetByPeriodAsync(
                WeatherSource.OpenWeather,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);
    }

    private static WeatherReading CreateReading(
        DateOnly date,
        double heatIndex)
    {
        var localDate = date.ToDateTime(new TimeOnly(12, 0));

        var measurementDate = TimeZoneInfo.ConvertTimeToUtc(
            localDate,
            BrazilTimeZone);

        return new WeatherReading
        {
            Source = WeatherSource.OpenWeather,
            Status = ReadingStatus.Complete,
            MeasurementDate = measurementDate,
            CollectionDate = DateTime.UtcNow,
            HeatIndexCelsius = heatIndex
        };
    }

    private static DailyForecast CreateForecast(
        DateOnly date,
        double heatIndex,
        DateTime? emissionDate = null)
    {
        return new DailyForecast
        {
            Source = WeatherSource.OpenWeather,
            ForecastDate = date.ToDateTime(TimeOnly.MinValue),
            ForecastEmissionDate = emissionDate ?? DateTime.UtcNow,
            EstimatedHeatIndexCelsius = heatIndex
        };
    }

    private static DateOnly Today()
    {
        var now = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            BrazilTimeZone);

        return DateOnly.FromDateTime(now.DateTime);
    }
}