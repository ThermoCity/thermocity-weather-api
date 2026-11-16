using System.Net;
using System.Net.Http.Json;
using Moq;
using ThermoCity.Weather.Api.Models.Auth;
using ThermoCity.Weather.Domain.Entities;
using ThermoCity.Weather.IntegrationTests.Fixtures;
using Xunit;

namespace ThermoCity.Weather.IntegrationTests;

public class AuthEndpointsTests
{
    [Fact]
    public async Task Register_ShouldReturnCreated_WhenDataIsValid()
    {
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        factory.UserRepositoryMock
            .Setup(u => u.FindByUsernameAsync("lucas", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        factory.UserRepositoryMock
            .Setup(u => u.InsertAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => u.Id = "user-abc")
            .Returns(Task.CompletedTask);

        var response = await client.PostAsJsonAsync("/api/auth/register", new AuthRequest
        {
            Username = "lucas",
            Password = "SecurePassword123"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUsernameOrPasswordIsEmpty()
    {
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new AuthRequest
        {
            Username = "",
            Password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Nome de usuário e senha são obrigatórios.", content);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenUsernameAlreadyExists()
    {
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        factory.UserRepositoryMock
            .Setup(u => u.FindByUsernameAsync("existingUser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = "existing", Username = "existingUser", PasswordHash = "hash" });

        var response = await client.PostAsJsonAsync("/api/auth/register", new AuthRequest
        {
            Username = "existingUser",
            Password = "SecurePassword123"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("já está em uso", content);
    }

    [Fact]
    public async Task SignIn_ShouldReturnBadRequest_WhenUsernameOrPasswordIsEmpty()
    {
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/signin", new AuthRequest
        {
            Username = "",
            Password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Nome de usuário e senha são obrigatórios.", content);
    }

    [Fact]
    public async Task SignIn_ShouldReturnUnauthorized_WhenUserNotFound()
    {
        using var factory = new ThermoCityApiFactory();
        using var client = factory.CreateClient();

        factory.UserRepositoryMock
            .Setup(u => u.FindByUsernameAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var response = await client.PostAsJsonAsync("/api/auth/signin", new AuthRequest
        {
            Username = "nonexistent",
            Password = "Password123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
