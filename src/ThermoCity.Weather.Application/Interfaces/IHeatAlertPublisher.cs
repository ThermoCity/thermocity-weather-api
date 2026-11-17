
namespace ThermoCity.Weather.Application.Interfaces;

public interface IHeatAlertPublisher
{
    Task PublishAsync(
        string message,
        CancellationToken cancellationToken);
}
