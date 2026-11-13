
using MongoDB.Driver;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.Domain.Enums;

namespace ThermoCity.Weather.Infrastructure.Mongo.Repositories;

public class WeatherReadingRepository : IWeatherReadingRepository
{
    private readonly IMongoCollection<WeatherReading> _collection;

    public WeatherReadingRepository(IMongoDatabase database)
    {
        _collection = database
            .GetCollection<WeatherReading>("TMC_LEITURA_CLIMA");
    }

    public async Task UpsertAsync(
        WeatherReading reading,
        CancellationToken cancellationToken)
    {
        var filter = Builders<WeatherReading>.Filter.And(
            Builders<WeatherReading>.Filter.Eq(
                x => x.Source, reading.Source),
            Builders<WeatherReading>.Filter.Eq(
                x => x.MeasurementDate, reading.MeasurementDate)
        );

        await _collection.ReplaceOneAsync(
            filter,
            reading,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<WeatherReading?> GetLatestAsync(
        WeatherSource source,
        CancellationToken cancellationToken)
    {
        return await _collection
            .Find(x => x.Source == source)
            .SortByDescending(x => x.MeasurementDate)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WeatherReading>> GetByPeriodAsync(
        WeatherSource source,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var filter = Builders<WeatherReading>.Filter.And(
            Builders<WeatherReading>.Filter.Eq(
                x => x.Source, source),
            Builders<WeatherReading>.Filter.Gte(
                x => x.MeasurementDate, startDate),
            Builders<WeatherReading>.Filter.Lt(
                x => x.MeasurementDate, endDate)
        );

        return await _collection
            .Find(filter)
            .SortBy(x => x.MeasurementDate)
            .ToListAsync(cancellationToken);
    }
}
