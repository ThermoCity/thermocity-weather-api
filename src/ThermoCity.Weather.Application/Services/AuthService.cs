using ThermoCity.Weather.Application.Exceptions;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Models;
using ThermoCity.Weather.Domain.Entities;

namespace ThermoCity.Weather.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> RegisterAsync(
        AuthCredentials credentials)
    {
        var existing = await _userRepository.FindByUsernameAsync(credentials.Username);

        if (existing is not null)
            throw new ConflictException(
                $"O nome de usuário '{credentials.Username}' já está em uso.");

        var user = new User
        {
            Username = credentials.Username,
            PasswordHash = _passwordHasher.Hash(credentials.Password)
        };

        await _userRepository.InsertAsync(user);

        return user;
    }

    public async Task<User?> ValidateCredentialsAsync(
        AuthCredentials credentials)
    {
        var user = await _userRepository.FindByUsernameAsync(credentials.Username);

        if (user is null)
            return null;

        return _passwordHasher.Verify(credentials.Password, user.PasswordHash)
            ? user
            : null;
    }
}
