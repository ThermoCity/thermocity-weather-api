using Moq;
using ThermoCity.Weather.Application.Exceptions;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Models;
using ThermoCity.Weather.Application.Services;
using ThermoCity.Weather.Domain.Entities;
using Xunit;

namespace ThermoCity.Weather.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_userRepositoryMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowConflictException_WhenUsernameAlreadyExists()
    {
        var credentials = new AuthCredentials("existingUser", "password123");
        var existingUser = new User { Username = "existingUser" };

        _userRepositoryMock
            .Setup(r => r.FindByUsernameAsync("existingUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        await Assert.ThrowsAsync<ConflictException>(() => _service.RegisterAsync(credentials));
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateAndSaveUser_WhenUsernameIsAvailable()
    {
        var credentials = new AuthCredentials("newUser", "password123");

        _userRepositoryMock
            .Setup(r => r.FindByUsernameAsync("newUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock
            .Setup(h => h.Hash("password123"))
            .Returns("hashedPassword");

        var user = await _service.RegisterAsync(credentials);

        Assert.Equal("newUser", user.Username);
        Assert.Equal("hashedPassword", user.PasswordHash);
        _userRepositoryMock.Verify(r => r.InsertAsync(It.Is<User>(u => u.Username == "newUser"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldReturnNull_WhenUserNotFound()
    {
        var credentials = new AuthCredentials("notFound", "pwd");

        _userRepositoryMock
            .Setup(r => r.FindByUsernameAsync("notFound", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.ValidateCredentialsAsync(credentials);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldReturnNull_WhenPasswordIsIncorrect()
    {
        var credentials = new AuthCredentials("user", "wrongPwd");
        var user = new User { Username = "user", PasswordHash = "hashed" };

        _userRepositoryMock
            .Setup(r => r.FindByUsernameAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.Verify("wrongPwd", "hashed"))
            .Returns(false);

        var result = await _service.ValidateCredentialsAsync(credentials);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_ShouldReturnUser_WhenPasswordMatches()
    {
        var credentials = new AuthCredentials("user", "correctPwd");
        var user = new User { Username = "user", PasswordHash = "hashed" };

        _userRepositoryMock
            .Setup(r => r.FindByUsernameAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(h => h.Verify("correctPwd", "hashed"))
            .Returns(true);

        var result = await _service.ValidateCredentialsAsync(credentials);

        Assert.NotNull(result);
        Assert.Equal("user", result.Username);
    }

    [Theory]
    [InlineData("", "pwd")]
    [InlineData("user", "")]
    public async Task RegisterAsync_ShouldThrowInvalidInputException_WhenCredentialsAreEmpty(string username, string password)
    {
        var credentials = new AuthCredentials(username, password);

        await Assert.ThrowsAsync<InvalidInputException>(() => _service.RegisterAsync(credentials));
    }

    [Theory]
    [InlineData("", "pwd")]
    [InlineData("user", "")]
    public async Task ValidateCredentialsAsync_ShouldThrowInvalidInputException_WhenCredentialsAreEmpty(string username, string password)
    {
        var credentials = new AuthCredentials(username, password);

        await Assert.ThrowsAsync<InvalidInputException>(() => _service.ValidateCredentialsAsync(credentials));
    }
}
