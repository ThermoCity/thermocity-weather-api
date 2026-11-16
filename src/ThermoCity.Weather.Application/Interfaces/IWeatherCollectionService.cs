
namespace ThermoCity.Weather.Application.Interfaces;

public interface IWeatherCollectionService
{
    Task CollectAsync(CancellationToken cancellationToken);
}
