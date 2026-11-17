
using ThermoCity.Weather.Application.Interfaces;

namespace ThermoCity.Weather.Worker.Workers;

public class HeatAlertMonitoringWorker : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHeatAlertPublisher _heatAlertPublisher;
    private readonly ILogger<HeatAlertMonitoringWorker> _logger;
    private readonly TimeSpan _interval;

    public HeatAlertMonitoringWorker(
        IHttpClientFactory httpClientFactory,
        IHeatAlertPublisher heatAlertPublisher,
        IConfiguration configuration,
        ILogger<HeatAlertMonitoringWorker> logger)
    {
        _httpClientFactory = httpClientFactory;
        _heatAlertPublisher = heatAlertPublisher;
        _logger = logger;

        var intervalHours = configuration.GetValue<int>(
            "Worker:HeatAlertMonitoringIntervalHours", 1);

        _interval = TimeSpan.FromHours(intervalHours);
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Heat alert monitoring started. Interval: {Interval}",
            _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishHeatAlertsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Error while monitoring heat alerts.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Heat alert monitoring stopped.");
    }

    private async Task PublishHeatAlertsAsync(
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient("HeatAlertsApi");

        using var response = await httpClient.GetAsync(
            "api/heat-alerts",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogWarning(
                "Heat alerts endpoint returned an empty response.");

            return;
        }

        await _heatAlertPublisher.PublishAsync(
            json,
            cancellationToken);

        _logger.LogInformation(
            "Heat alert data successfully published to RabbitMQ.");
    }
}
