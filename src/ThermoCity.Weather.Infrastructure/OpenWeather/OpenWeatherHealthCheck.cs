using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ThermoCity.Weather.Infrastructure.OpenWeather;

public class OpenWeatherHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenWeatherHealthCheck(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiKey = _configuration["OpenWeather:ApiKey"] ?? string.Empty;
            var response = await _httpClient.GetAsync(
                $"weather?lat=-22.12&lon=-51.39&appid={apiKey}",
                cancellationToken);

            // Se o serviço responder (inclusive 401 Unauthorized por falta de chave em ambiente local/testes),
            // significa que o serviço externo está ativo, acessível e respondendo na rede.
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return HealthCheckResult.Healthy("Serviço externo OpenWeather está acessível.");
            }

            return HealthCheckResult.Degraded(
                $"OpenWeather respondeu com status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Não foi possível conectar ao serviço externo OpenWeather.",
                ex);
        }
    }
}
