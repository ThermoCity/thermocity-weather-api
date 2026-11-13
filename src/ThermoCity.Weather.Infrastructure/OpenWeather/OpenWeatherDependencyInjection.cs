using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThermoCity.Weather.Application.Interfaces;

namespace ThermoCity.Weather.Infrastructure.OpenWeather;

public static class OpenWeatherDependencyInjection
{
    public static IServiceCollection AddOpenWeather(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var baseUrl = configuration["OpenWeather:BaseUrl"]
            ?? throw new InvalidOperationException(
                "A URL da OpenWeather não foi configurada.");

        services.AddHttpClient<IWeatherProvider, OpenWeatherProvider>(
            client =>
            {
                client.BaseAddress = new Uri(baseUrl);
            });

        return services;
    }
}