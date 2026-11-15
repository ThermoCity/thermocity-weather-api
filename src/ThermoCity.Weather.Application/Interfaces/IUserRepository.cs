using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task InsertAsync(User user, CancellationToken cancellationToken = default);
}
