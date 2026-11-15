using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThermoCity.Weather.Application.Exceptions;
using ThermoCity.Weather.Application.Interfaces;
using ThermoCity.Weather.Application.Models;
using ThermoCity.Weather.Api.Models.Auth;

namespace ThermoCity.Weather.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Registra um novo usuário.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidInputException("Nome de usuário e senha são obrigatórios.");

        _logger.LogInformation(
            "Recebida solicitação para cadastrar usuário {Username}",
            request.Username);

        var user = await _authService.RegisterAsync(
            new AuthCredentials(request.Username, request.Password));

        _logger.LogInformation(
            "Cadastro do usuário {Username} concluído",
            user.Username);

        return Created($"/api/Auth/users/{user.Id}", new { user.Username });
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT.
    /// </summary>
    [HttpPost("signin")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn(AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidInputException("Nome de usuário e senha são obrigatórios.");

        _logger.LogInformation(
            "Recebida tentativa de autenticação do usuário {Username}",
            request.Username);

        var user = await _authService.ValidateCredentialsAsync(
            new AuthCredentials(request.Username, request.Password));

        if (user is null)
        {
            _logger.LogWarning(
                "Autenticação recusada para o usuário {Username}",
                request.Username);

            return Unauthorized();
        }

        var token = _tokenService.GenerateToken(user.Username);

        _logger.LogInformation(
            "Autenticação do usuário {Username} concluída",
            user.Username);

        return Ok(new AuthResponse(token));
    }
}
