
namespace ThermoCity.Weather.Application.Interfaces;

public interface IForecastCollectionService
{
    Task CollectAsync(CancellationToken cancellationToken);
}
