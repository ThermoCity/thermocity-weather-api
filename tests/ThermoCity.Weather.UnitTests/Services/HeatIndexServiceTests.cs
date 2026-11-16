using ThermoCity.Weather.Application.Services;

namespace ThermoCity.Weather.UnitTests.Services;

public class HeatIndexServiceTests
{
    private readonly HeatIndexService _service = new();

    [Theory]
    [InlineData(25, 50, 24.8611)]
    [InlineData(30, 70, 35.0380)]
    [InlineData(32, 90, 48.9524)]
    public void Calculate_ShouldReturnExpectedHeatIndex(
        double temperature,
        double humidity,
        double expected)
    {
        // Act
        var result = _service.Calculate(temperature, humidity);

        // Assert
        Assert.Equal(expected, result, 3);
    }
}
