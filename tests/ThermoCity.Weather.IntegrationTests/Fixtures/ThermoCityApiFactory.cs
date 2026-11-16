using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.IntegrationTests.Fixtures;

public class ThermoCityApiFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "integration-tests-super-secret-key-at-least-32-chars";
    public const string TestIssuer = "thermocity-weather-api";
    public const string TestAudience = "thermocity-weather-clients";

    public Mock<IWeatherReadingRepository> WeatherReadingRepositoryMock { get; } = new();

    public Mock<IDailyForecastRepository> DailyForecastRepositoryMock { get; } = new();

    public Mock<IDailyWeatherSummaryRepository> DailyWeatherSummaryRepositoryMock { get; } = new();

    public Mock<IUserRepository> UserRepositoryMock { get; } = new();

    public HttpClient CreateAuthenticatedClient(string username = "testuser")
    {
        var client = CreateClient();
        var token = GenerateTestToken(username);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static string GenerateTestToken(string username = "testuser")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            TestIssuer,
            TestAudience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MongoDb:ConnectionString"] = "mongodb://localhost:27017",
                ["MongoDb:DatabaseName"] = "thermocity-weather",
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWeatherReadingRepository>();
            services.RemoveAll<IDailyForecastRepository>();
            services.RemoveAll<IDailyWeatherSummaryRepository>();
            services.RemoveAll<IUserRepository>();

            services.AddSingleton(WeatherReadingRepositoryMock.Object);
            services.AddSingleton(DailyForecastRepositoryMock.Object);
            services.AddSingleton(DailyWeatherSummaryRepositoryMock.Object);
            services.AddSingleton(UserRepositoryMock.Object);
        });
    }
}
