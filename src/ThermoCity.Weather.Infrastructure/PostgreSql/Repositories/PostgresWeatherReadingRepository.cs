
using Microsoft.EntityFrameworkCore;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;
using ThermoCity.Weather.Infrastructure.PostgreSql;

namespace ThermoCity.Weather.Infrastructure.PostgreSql.Repositories;

public class PostgresWeatherReadingRepository(
    ThermoCityDbContext context) : IWeatherReadingRepository
{
    public async Task UpsertAsync(
        WeatherReading reading,
        CancellationToken cancellationToken)
    {
        var existing = await context.WeatherReadings
            .FirstOrDefaultAsync(
                x => x.Source == reading.Source &&
                     x.MeasurementDate == reading.MeasurementDate,
                cancellationToken);

        if (existing is null)
        {
            await context.WeatherReadings.AddAsync(
                reading,
                cancellationToken);
        }
        else
        {
            var existingId = existing.Id;

            context.Entry(existing).CurrentValues.SetValues(reading);

            existing.Id = existingId;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<WeatherReading?> GetLatestAsync(
        WeatherSource source,
        CancellationToken cancellationToken)
    {
        return await context.WeatherReadings
            .AsNoTracking()
            .Where(x => x.Source == source)
            .OrderByDescending(x => x.MeasurementDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WeatherReading>> GetByPeriodAsync(
        WeatherSource source,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        return await context.WeatherReadings
            .AsNoTracking()
            .Where(x =>
                x.Source == source &&
                x.MeasurementDate >= startDate &&
                x.MeasurementDate < endDate)
            .OrderBy(x => x.MeasurementDate)
            .ToListAsync(cancellationToken);
    }
}
