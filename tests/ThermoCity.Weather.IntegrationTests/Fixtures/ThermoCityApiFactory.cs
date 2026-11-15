
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.IntegrationTests.Fixtures;

public class ThermoCityApiFactory : WebApplicationFactory<Program>
{
    public Mock<IWeatherReadingRepository> WeatherReadingRepositoryMock { get; } = new();

    public Mock<IDailyForecastRepository> DailyForecastRepositoryMock { get; } = new();

    public Mock<IDailyWeatherSummaryRepository> DailyWeatherSummaryRepositoryMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "thermocity-weather",
                ["Jwt:Key"] = "integration-tests-super-secret-key-at-least-32-chars",
                ["Jwt:Issuer"] = "thermocity-weather-api",
                ["Jwt:Audience"] = "thermocity-weather-clients"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWeatherReadingRepository>();
            services.RemoveAll<IDailyForecastRepository>();
            services.RemoveAll<IDailyWeatherSummaryRepository>();

            services.AddSingleton(WeatherReadingRepositoryMock.Object);
            services.AddSingleton(DailyForecastRepositoryMock.Object);
            services.AddSingleton(DailyWeatherSummaryRepositoryMock.Object);
        });
    }
}
