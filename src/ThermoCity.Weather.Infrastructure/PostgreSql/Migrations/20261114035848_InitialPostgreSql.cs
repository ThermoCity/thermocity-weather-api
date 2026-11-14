using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThermoCity.Weather.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_forecasts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ForecastDate = table.Column<DateTime>(type: "date", nullable: false),
                    ForecastEmissionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MinimumTemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    MaximumTemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    DayTemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    DayFeelsLikeCelsius = table.Column<double>(type: "double precision", nullable: true),
                    HumidityPercentage = table.Column<double>(type: "double precision", nullable: true),
                    PressureHpa = table.Column<double>(type: "double precision", nullable: true),
                    WindSpeedMs = table.Column<double>(type: "double precision", nullable: true),
                    WeatherDescription = table.Column<string>(type: "text", nullable: true),
                    EstimatedHeatIndexCelsius = table.Column<double>(type: "double precision", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_forecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "daily_weather_summaries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReferenceDate = table.Column<DateTime>(type: "date", nullable: false),
                    MinimumTemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    AverageTemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    MaximumTemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    MinimumHumidityPercentage = table.Column<double>(type: "double precision", nullable: true),
                    AverageHumidityPercentage = table.Column<double>(type: "double precision", nullable: true),
                    MaximumHumidityPercentage = table.Column<double>(type: "double precision", nullable: true),
                    MinimumHeatIndexCelsius = table.Column<double>(type: "double precision", nullable: true),
                    AverageHeatIndexCelsius = table.Column<double>(type: "double precision", nullable: true),
                    MaximumHeatIndexCelsius = table.Column<double>(type: "double precision", nullable: true),
                    ObservationCount = table.Column<int>(type: "integer", nullable: false),
                    ConsolidationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_weather_summaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "weather_readings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MeasurementDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TemperatureCelsius = table.Column<double>(type: "double precision", nullable: true),
                    FeelsLikeCelsius = table.Column<double>(type: "double precision", nullable: true),
                    HumidityPercentage = table.Column<double>(type: "double precision", nullable: true),
                    PressureHpa = table.Column<double>(type: "double precision", nullable: true),
                    WindSpeedMs = table.Column<double>(type: "double precision", nullable: true),
                    WeatherDescription = table.Column<string>(type: "text", nullable: true),
                    HeatIndexCelsius = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_readings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_forecasts_Source_ForecastEmissionDate_ForecastDate",
                table: "daily_forecasts",
                columns: new[] { "Source", "ForecastEmissionDate", "ForecastDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_daily_weather_summaries_Source_ReferenceDate",
                table: "daily_weather_summaries",
                columns: new[] { "Source", "ReferenceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weather_readings_Source_MeasurementDate",
                table: "weather_readings",
                columns: new[] { "Source", "MeasurementDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_forecasts");

            migrationBuilder.DropTable(
                name: "daily_weather_summaries");

            migrationBuilder.DropTable(
                name: "weather_readings");
        }
    }
}
