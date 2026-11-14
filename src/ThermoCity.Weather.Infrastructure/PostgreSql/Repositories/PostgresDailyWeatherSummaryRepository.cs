
using Microsoft.EntityFrameworkCore;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using ThermoCity.Weather.Infrastructure.PostgreSql;

namespace ThermoCity.Weather.Infrastructure.PostgreSql.Repositories;

public class PostgresDailyWeatherSummaryRepository(
    ThermoCityDbContext context) : IDailyWeatherSummaryRepository
{
    public async Task UpsertAsync(
        DailyWeatherSummary summary,
        CancellationToken cancellationToken)
    {
        var existing = await context.DailyWeatherSummaries
            .FirstOrDefaultAsync(
                x => x.Source == summary.Source &&
                     x.ReferenceDate == summary.ReferenceDate,
                cancellationToken);

        if (existing is null)
        {
            await context.DailyWeatherSummaries.AddAsync(
                summary,
                cancellationToken);
        }
        else
        {
            var existingId = existing.Id;

            context.Entry(existing).CurrentValues.SetValues(summary);

            existing.Id = existingId;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DailyWeatherSummary?> GetByDateAsync(
        WeatherSource source,
        DateTime referenceDate,
        CancellationToken cancellationToken)
    {
        return await context.DailyWeatherSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Source == source &&
                     x.ReferenceDate == referenceDate,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<DailyWeatherSummary>> GetByPeriodAsync(
        WeatherSource source,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return await context.DailyWeatherSummaries
            .AsNoTracking()
            .Where(x =>
                x.Source == source &&
                x.ReferenceDate >= startDate &&
                x.ReferenceDate < endDate)
            .OrderBy(x => x.ReferenceDate)
            .ToListAsync(cancellationToken);
    }
}
