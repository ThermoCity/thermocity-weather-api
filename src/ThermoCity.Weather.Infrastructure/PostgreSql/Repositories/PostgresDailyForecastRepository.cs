
using Microsoft.EntityFrameworkCore;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using ThermoCity.Weather.Infrastructure.PostgreSql;

namespace ThermoCity.Weather.Infrastructure.PostgreSql.Repositories;

public class PostgresDailyForecastRepository(
    ThermoCityDbContext context) : IDailyForecastRepository
{
    public async Task UpsertManyAsync(
        IEnumerable<DailyForecast> forecasts,
        CancellationToken cancellationToken)
    {
        foreach (var forecast in forecasts)
        {
            var existing = await context.DailyForecasts
                .FirstOrDefaultAsync(
                    x => x.Source == forecast.Source &&
                         x.ForecastEmissionDate == forecast.ForecastEmissionDate &&
                         x.ForecastDate == forecast.ForecastDate,
                    cancellationToken);

            if (existing is null)
            {
                await context.DailyForecasts.AddAsync(
                    forecast,
                    cancellationToken);
            }
            else
            {
                var existingId = existing.Id;

                context.Entry(existing).CurrentValues.SetValues(forecast);

                existing.Id = existingId;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DailyForecast>> GetByPeriodAsync(
        WeatherSource source,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return await context.DailyForecasts
            .AsNoTracking()
            .Where(x =>
                x.Source == source &&
                x.ForecastDate >= startDate &&
                x.ForecastDate < endDate)
            .OrderBy(x => x.ForecastDate)
            .ToListAsync(cancellationToken);
    }
}
