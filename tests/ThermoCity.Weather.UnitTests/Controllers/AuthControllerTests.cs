using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ThermoCity.Weather.Api.Controllers;
using ThermoCity.Weather.Api.Models.Auth;
using ThermoCity.Weather.Application.Exceptions;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Models;
using ThermoCity.Weather.Domain.Entities;
using Xunit;

namespace ThermoCity.Weather.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<ILogger<AuthController>> _loggerMock = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(
            _authServiceMock.Object,
            _tokenServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Register_ShouldReturnCreated_WhenRegistrationSucceeds()
    {
        var request = new AuthRequest { Username = "lucas", Password = "Password123!" };
        var createdUser = new User { Id = "user-123", Username = "lucas", PasswordHash = "hashed" };

        _authServiceMock
            .Setup(s => s.RegisterAsync(It.Is<AuthCredentials>(c => c.Username == "lucas" && c.Password == "Password123!")))
            .ReturnsAsync(createdUser);

        var result = await _controller.Register(request);

        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.Equal("/api/Auth/users/user-123", createdResult.Location);
    }

    [Fact]
    public async Task SignIn_ShouldReturnOkWithToken_WhenCredentialsAreValid()
    {
        var request = new AuthRequest { Username = "lucas", Password = "Password123!" };
        var user = new User { Id = "user-123", Username = "lucas", PasswordHash = "hashed" };

        _authServiceMock
            .Setup(s => s.ValidateCredentialsAsync(It.Is<AuthCredentials>(c => c.Username == "lucas" && c.Password == "Password123!")))
            .ReturnsAsync(user);

        _tokenServiceMock
            .Setup(t => t.GenerateToken("lucas"))
            .Returns("valid-jwt-token");

        var result = await _controller.SignIn(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal("valid-jwt-token", response.Token);
    }

    [Fact]
    public async Task SignIn_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
    {
        var request = new AuthRequest { Username = "lucas", Password = "WrongPassword" };

        _authServiceMock
            .Setup(s => s.ValidateCredentialsAsync(It.IsAny<AuthCredentials>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.SignIn(request);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Theory]
    [InlineData("", "pwd")]
    [InlineData("user", "")]
    [InlineData("   ", "pwd")]
    public async Task Register_ShouldThrowInvalidInputException_WhenCredentialsAreEmpty(string username, string password)
    {
        var request = new AuthRequest { Username = username, Password = password };

        await Assert.ThrowsAsync<InvalidInputException>(() => _controller.Register(request));
    }

    [Theory]
    [InlineData("", "pwd")]
    [InlineData("user", "")]
    public async Task SignIn_ShouldThrowInvalidInputException_WhenCredentialsAreEmpty(string username, string password)
    {
        var request = new AuthRequest { Username = username, Password = password };

        await Assert.ThrowsAsync<InvalidInputException>(() => _controller.SignIn(request));
    }
}
