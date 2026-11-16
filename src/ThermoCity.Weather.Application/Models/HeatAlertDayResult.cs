namespace ThermoCity.Weather.Application.Models;

public class HeatAlertDayResult
{
    public DateOnly Date { get; set; }
    public int? Level { get; set; }
    public string? Name { get; set; }
    public double? MaximumHeatIndexCelsius { get; set; }
    public string Origin { get; set; } = string.Empty;
}