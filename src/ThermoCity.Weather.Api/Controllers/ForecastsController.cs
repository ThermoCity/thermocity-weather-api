
using Microsoft.AspNetCore.Mvc;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Api.Controllers;

[ApiController]
[Route("api/forecasts")]
public class ForecastsController : ControllerBase
{
    private readonly IDailyForecastRepository _repository;

    public ForecastsController(IDailyForecastRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Consulta previsões meteorológicas por período.
    /// As datas representam dias do calendário local da cidade.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DailyForecast>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<DailyForecast>>> GetByPeriod(
        [FromQuery] WeatherSource source,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            return BadRequest("A data final deve ser igual ou posterior à inicial.");

        var forecasts = await _repository.GetByPeriodAsync(
            source,
            startDate.ToDateTime(TimeOnly.MinValue),
            endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
            cancellationToken);

        return Ok(forecasts);
    }
}
