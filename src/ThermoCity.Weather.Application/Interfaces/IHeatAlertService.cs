using ThermoCity.Weather.Application.Models;

namespace ThermoCity.Weather.Application.Interfaces;

public interface IHeatAlertService
{
    Task<HeatAlertResult> GetHeatAlertsAsync(
        CancellationToken cancellationToken);
}