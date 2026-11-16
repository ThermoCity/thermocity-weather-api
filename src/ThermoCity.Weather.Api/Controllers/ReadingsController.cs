
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThermoCity.Weather.Application.Exceptions;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/readings")]
public class ReadingsController : ControllerBase
{
    private readonly IWeatherReadingRepository _repository;

    public ReadingsController(IWeatherReadingRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Retorna a observação meteorológica mais recente.
    /// </summary>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(WeatherReading), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherReading>> GetLatest(
        [FromQuery] WeatherSource source = WeatherSource.OpenWeather,
        CancellationToken cancellationToken = default)
    {
        var reading = await _repository.GetLatestAsync(
            source,
            cancellationToken);

        if (reading is null)
            return NotFound("Nenhuma observação encontrada.");

        return Ok(reading);
    }

    /// <summary>
    /// Consulta observações meteorológicas por período.
    /// As datas devem ser informadas em UTC.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<WeatherReading>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<WeatherReading>>> GetByPeriod(
        [FromQuery] WeatherSource source,
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate <= startDate)
            throw new InvalidInputException("A data final deve ser posterior à data inicial.");

        var readings = await _repository.GetByPeriodAsync(
            source,
            startDate.UtcDateTime,
            endDate.UtcDateTime,
            cancellationToken);

        return Ok(readings);
    }
}
