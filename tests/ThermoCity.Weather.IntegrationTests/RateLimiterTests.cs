using System.Net;
using ThermoCity.Weather.IntegrationTests.Fixtures;
using Xunit;

namespace ThermoCity.Weather.IntegrationTests;

public class RateLimiterTests
{
    [Fact]
    public async Task RateLimiter_ShouldReturn429TooManyRequests_WhenLimitExceeded()
    {
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        HttpResponseMessage lastResponse = null!;

        // O limite configurado é de 10 requisições por janela. Enviamos 12 requisições.
        for (int i = 0; i < 12; i++)
        {
            lastResponse = await client.GetAsync("/health/live");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        var content = await lastResponse.Content.ReadAsStringAsync();
        Assert.Contains("Limite de requisições excedido", content);
    }
}
