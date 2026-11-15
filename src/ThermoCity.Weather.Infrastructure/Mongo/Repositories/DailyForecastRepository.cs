
using MongoDB.Bson;
using MongoDB.Driver;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Infrastructure.Mongo.Repositories;

public class DailyForecastRepository : IDailyForecastRepository
{
    private readonly IMongoCollection<DailyForecast> _collection;

    public DailyForecastRepository(IMongoDatabase database)
    {
        _collection = database
            .GetCollection<DailyForecast>("TMC_PREVISAO_DIARIA");
    }

    public async Task UpsertManyAsync(
        IEnumerable<DailyForecast> forecasts,
        CancellationToken cancellationToken)
    {
        var operations = new List<WriteModel<DailyForecast>>();

        foreach (var forecast in forecasts)
        {
            var filter =
                Builders<DailyForecast>.Filter.Eq(x => x.Source, forecast.Source) &
                Builders<DailyForecast>.Filter.Eq(x => x.ForecastEmissionDate, forecast.ForecastEmissionDate) &
                Builders<DailyForecast>.Filter.Eq(x => x.ForecastDate, forecast.ForecastDate);

            var existing = await _collection
                .Find(filter)
                .FirstOrDefaultAsync(cancellationToken);

            forecast.Id = existing?.Id ?? ObjectId.GenerateNewId();

            operations.Add(
                new ReplaceOneModel<DailyForecast>(filter, forecast)
                {
                    IsUpsert = true
                });
        }

        if (operations.Count == 0)
            return;

        await _collection.BulkWriteAsync(
            operations,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<DailyForecast>> GetByPeriodAsync(
        WeatherSource source,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var filter = Builders<DailyForecast>.Filter.And(
            Builders<DailyForecast>.Filter.Eq(
                x => x.Source, source),
            Builders<DailyForecast>.Filter.Gte(
                x => x.ForecastDate, startDate),
            Builders<DailyForecast>.Filter.Lt(
                x => x.ForecastDate, endDate)
        );

        return await _collection
            .Find(filter)
            .SortBy(x => x.ForecastDate)
            .ToListAsync(cancellationToken);
    }

}
