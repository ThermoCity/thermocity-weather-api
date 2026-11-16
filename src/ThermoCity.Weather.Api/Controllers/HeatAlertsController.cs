using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThermoCity.Weather.Api.DTOs;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Models;

namespace ThermoCity.Weather.Api.Controllers;

[ApiController]
[Route("api/heat-alerts")]
[AllowAnonymous]
public class HeatAlertsController : ControllerBase
{
    private readonly IHeatAlertService _heatAlertService;

    public HeatAlertsController(IHeatAlertService heatAlertService)
    {
        _heatAlertService = heatAlertService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result = await _heatAlertService.GetHeatAlertsAsync(
            cancellationToken);

        var response = new HeatAlertResponse
        {
            Hoje = MapDay(result.Today),
            ProximosDias = result.UpcomingDays
                .Select(day => MapDay(day)!)
                .ToList(),
            Criterio = result.Criterion,
            AcaoEsperada = result.ExpectedAction,
            VersaoLimiar = result.ThresholdVersion,
            AtualizadoEm = result.UpdatedAt,
            Desatualizado = result.IsStale
        };

        return Ok(response);
    }

    private static HeatAlertDay? MapDay(HeatAlertDayResult? result)
    {
        if (result is null)
            return null;

        return new HeatAlertDay
        {
            Data = result.Date,
            Nivel = result.Level,
            Nome = result.Name,
            IcMaxC = result.MaximumHeatIndexCelsius,
            Origem = result.Origin
        };
    }
}