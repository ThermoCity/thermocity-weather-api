
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ThermoCity.Weather.Infrastructure.Mongo;

public class MongoDbHealthCheck : IHealthCheck
{
    private readonly IMongoClient _mongoClient;

    public MongoDbHealthCheck(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _mongoClient
                .GetDatabase("admin")
                .RunCommandAsync<BsonDocument>(
                    new BsonDocument("ping", 1),
                    cancellationToken: cancellationToken
                );

            return HealthCheckResult.Healthy(
                "MongoDB está conectado."
            );
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "MongoDB não está conectado.",
                ex
            );
        }
    }
}
