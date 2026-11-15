using MongoDB.Driver;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.Infrastructure.Mongo.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _collection;

    public UserRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<User>("users");
    }

    public async Task<User?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return await _collection
            .Find(u => u.Username == username)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task InsertAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _collection.InsertOneAsync(user, cancellationToken: cancellationToken);
    }
}
