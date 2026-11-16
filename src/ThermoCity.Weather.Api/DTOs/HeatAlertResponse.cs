using System.Text.Json.Serialization;

namespace ThermoCity.Weather.Api.DTOs;

public class HeatAlertResponse
{
    [JsonPropertyName("hoje")]
    public HeatAlertDay? Hoje { get; set; }

    [JsonPropertyName("proximosDias")]
    public IReadOnlyCollection<HeatAlertDay> ProximosDias { get; set; } = [];

    [JsonPropertyName("criterio")]
    public string Criterio { get; set; } = string.Empty;

    [JsonPropertyName("acaoEsperada")]
    public string AcaoEsperada { get; set; } = string.Empty;

    [JsonPropertyName("versaoLimiar")]
    public string VersaoLimiar { get; set; } = string.Empty;

    [JsonPropertyName("atualizadoEm")]
    public DateTimeOffset AtualizadoEm { get; set; }

    [JsonPropertyName("desatualizado")]
    public bool Desatualizado { get; set; }
}

public class HeatAlertDay
{
    [JsonPropertyName("data")]
    public DateOnly Data { get; set; }

    [JsonPropertyName("nivel")]
    public int? Nivel { get; set; }

    [JsonPropertyName("nome")]
    public string? Nome { get; set; }

    [JsonPropertyName("icMaxC")]
    public double? IcMaxC { get; set; }

    [JsonPropertyName("origem")]
    public string Origem { get; set; } = string.Empty;
}