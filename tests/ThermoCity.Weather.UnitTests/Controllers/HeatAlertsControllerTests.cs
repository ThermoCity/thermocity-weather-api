
using Microsoft.AspNetCore.Mvc;
using Moq;
using ThermoCity.Weather.Api.Controllers;
using ThermoCity.Weather.Api.DTOs;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Models;

namespace ThermoCity.Weather.UnitTests.Controllers;

public class HeatAlertsControllerTests
{
    private readonly Mock<IHeatAlertService> _heatAlertService;
    private readonly HeatAlertsController _controller;

    public HeatAlertsControllerTests()
    {
        _heatAlertService = new Mock<IHeatAlertService>();

        _controller = new HeatAlertsController(
            _heatAlertService.Object);
    }

    [Fact]
    public async Task Get_ShouldReturnOkWithHeatAlerts()
    {
        // Arrange
        var result = CreateHeatAlertResult();

        _heatAlertService
            .Setup(service => service.GetHeatAlertsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.Get(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var responseDto = Assert.IsType<HeatAlertResponse>(okResult.Value);

        Assert.NotNull(responseDto.Hoje);
        Assert.Equal(4, responseDto.Hoje.Nivel);
        Assert.Equal("Alto risco", responseDto.Hoje.Nome);
        Assert.Equal(37.2, responseDto.Hoje.IcMaxC);
        Assert.Equal("OBSERVADO", responseDto.Hoje.Origem);

        Assert.Single(responseDto.ProximosDias);
        Assert.Equal("A707-2003-2026-v1", responseDto.VersaoLimiar);
        Assert.Equal("IC máx. ≥ 37,0 °C", responseDto.Criterio);
        Assert.False(responseDto.Desatualizado);

        _heatAlertService.Verify(
            service => service.GetHeatAlertsAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Get_ShouldReturnEmptyTodayWhenNoReadingsAreAvailable()
    {
        // Arrange
        var result = new HeatAlertResult
        {
            Today = null,
            UpcomingDays = [],
            Criterion = "Sem dados meteorológicos disponíveis",
            ExpectedAction = "Aguardando leitura meteorológica",
            ThresholdVersion = "A707-2003-2026-v1",
            IsStale = true
        };

        _heatAlertService
            .Setup(service => service.GetHeatAlertsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.Get(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var responseDto = Assert.IsType<HeatAlertResponse>(okResult.Value);

        Assert.Null(responseDto.Hoje);
        Assert.Empty(responseDto.ProximosDias);
        Assert.True(responseDto.Desatualizado);
        Assert.Equal(
            "Sem dados meteorológicos disponíveis",
            responseDto.Criterio);
    }

    [Fact]
    public async Task Get_ShouldMapUpcomingDaysCorrectly()
    {
        // Arrange
        var result = CreateHeatAlertResult();

        _heatAlertService
            .Setup(service => service.GetHeatAlertsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var response = await _controller.Get(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var responseDto = Assert.IsType<HeatAlertResponse>(okResult.Value);

        var upcomingDay = Assert.Single(responseDto.ProximosDias);

        Assert.Equal(new DateOnly(2026, 11, 18), upcomingDay.Data);
        Assert.Equal(3, upcomingDay.Nivel);
        Assert.Equal("Alerta", upcomingDay.Nome);
        Assert.Equal(36.4, upcomingDay.IcMaxC);
        Assert.Equal("PREVISTO", upcomingDay.Origem);
    }

    private static HeatAlertResult CreateHeatAlertResult()
    {
        return new HeatAlertResult
        {
            Today = new HeatAlertDayResult
            {
                Date = new DateOnly(2026, 11, 17),
                Level = 4,
                Name = "Alto risco",
                MaximumHeatIndexCelsius = 37.2,
                Origin = "OBSERVADO"
            },

            UpcomingDays =
            [
                new HeatAlertDayResult
                {
                    Date = new DateOnly(2026, 11, 18),
                    Level = 3,
                    Name = "Alerta",
                    MaximumHeatIndexCelsius = 36.4,
                    Origin = "PREVISTO"
                }
            ],

            Criterion = "IC máx. ≥ 37,0 °C",
            ExpectedAction = "Busca ativa ampliada",
            ThresholdVersion = "A707-2003-2026-v1",
            UpdatedAt = new DateTimeOffset(
                2026, 11, 17, 7, 2, 11,
                TimeSpan.FromHours(-3)),
            IsStale = false
        };
    }
}
