
using Microsoft.AspNetCore.Mvc;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Api.Controllers;

[ApiController]
[Route("api/daily-summaries")]
public class DailySummariesController : ControllerBase
{
    private readonly IDailyWeatherSummaryRepository _repository;

    public DailySummariesController(
        IDailyWeatherSummaryRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Consulta o resumo meteorológico de uma data.
    /// </summary>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(DailyWeatherSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DailyWeatherSummary>> GetByDate(
        DateOnly date,
        [FromQuery] WeatherSource source = WeatherSource.OpenWeather,
        CancellationToken cancellationToken = default)
    {
        var summary = await _repository.GetByDateAsync(
            source,
            date.ToDateTime(TimeOnly.MinValue),
            cancellationToken);

        if (summary is null)
            return NotFound("Nenhum resumo encontrado para essa data.");

        return Ok(summary);
    }

    /// <summary>
    /// Consulta os resumos meteorológicos por período.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<DailyWeatherSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<DailyWeatherSummary>>> GetByPeriod(
        [FromQuery] WeatherSource source,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            return BadRequest("A data final deve ser igual ou posterior à inicial.");

        var summaries = await _repository.GetByPeriodAsync(
            source,
            startDate.ToDateTime(TimeOnly.MinValue),
            endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
            cancellationToken);

        return Ok(summaries);
    }
}
