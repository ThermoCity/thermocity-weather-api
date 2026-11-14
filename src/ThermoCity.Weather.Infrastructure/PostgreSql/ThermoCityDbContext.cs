using Microsoft.EntityFrameworkCore;
using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.Infrastructure.PostgreSql;

public class ThermoCityDbContext(DbContextOptions<ThermoCityDbContext> options)
    : DbContext(options)
{
    public DbSet<WeatherReading> WeatherReadings => Set<WeatherReading>();
    public DbSet<DailyForecast> DailyForecasts => Set<DailyForecast>();
    public DbSet<DailyWeatherSummary> DailyWeatherSummaries => Set<DailyWeatherSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ThermoCityDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
