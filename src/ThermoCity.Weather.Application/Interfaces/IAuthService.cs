using ThermoCity.Weather.Application.Models;
using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.Application.Interfaces;

public interface IAuthService
{
    Task<User> RegisterAsync(AuthCredentials credentials);

    Task<User?> ValidateCredentialsAsync(AuthCredentials credentials);
}
