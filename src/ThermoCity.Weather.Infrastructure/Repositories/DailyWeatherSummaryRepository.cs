
using MongoDB.Bson;
using MongoDB.Driver;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Infrastructure.Repositories;

public class DailyWeatherSummaryRepository : IDailyWeatherSummaryRepository
{
    private readonly IMongoCollection<DailyWeatherSummary> _collection;

    public DailyWeatherSummaryRepository(IMongoDatabase database)
    {
        _collection = database
            .GetCollection<DailyWeatherSummary>("TMC_RESUMO_CLIMA_DIARIO");
    }

    public async Task UpsertAsync(
        DailyWeatherSummary summary,
        CancellationToken cancellationToken)
    {
        var filter =
            Builders<DailyWeatherSummary>.Filter.Eq(x => x.Source, summary.Source) &
            Builders<DailyWeatherSummary>.Filter.Eq(x => x.ReferenceDate, summary.ReferenceDate);

        var existing = await _collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);

        summary.Id = existing?.Id ?? ObjectId.GenerateNewId();

        await _collection.ReplaceOneAsync(
            filter,
            summary,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<DailyWeatherSummary?> GetByDateAsync(
        WeatherSource source,
        DateTime referenceDate,
        CancellationToken cancellationToken)
    {
        return await _collection
            .Find(x =>
                x.Source == source &&
                x.ReferenceDate == referenceDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<DailyWeatherSummary>> GetByPeriodAsync(
        WeatherSource source,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var filter = Builders<DailyWeatherSummary>.Filter.And(
            Builders<DailyWeatherSummary>.Filter.Eq(
                x => x.Source, source),
            Builders<DailyWeatherSummary>.Filter.Gte(
                x => x.ReferenceDate, startDate),
            Builders<DailyWeatherSummary>.Filter.Lt(
                x => x.ReferenceDate, endDate)
        );

        return await _collection
            .Find(filter)
            .SortBy(x => x.ReferenceDate)
            .ToListAsync(cancellationToken);
    }
}
