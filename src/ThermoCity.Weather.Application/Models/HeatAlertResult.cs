namespace ThermoCity.Weather.Application.Models;

public class HeatAlertResult
{
    public HeatAlertDayResult? Today { get; set; }

    public IReadOnlyCollection<HeatAlertDayResult> UpcomingDays { get; set; } = [];

    public string Criterion { get; set; } = string.Empty;

    public string ExpectedAction { get; set; } = string.Empty;

    public string ThresholdVersion { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsStale { get; set; }
}