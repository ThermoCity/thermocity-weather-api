
using ThermoCity.Weather.IntegrationTests.Fixtures;
using Xunit;

namespace ThermoCity.Weather.IntegrationTests;

public sealed class HealthCheckTests : IClassFixture<ThermoCityApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(ThermoCityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthLive_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("Healthy", content);
    }

    [Fact]
    public async Task GetSwagger_ShouldReturnSuccess()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);
    }
}