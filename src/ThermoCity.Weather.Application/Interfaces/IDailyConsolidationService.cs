namespace ThermoCity.Weather.Application.Interfaces;

public interface IDailyConsolidationService
{
    Task ConsolidatePreviousDayAsync(CancellationToken cancellationToken);
}