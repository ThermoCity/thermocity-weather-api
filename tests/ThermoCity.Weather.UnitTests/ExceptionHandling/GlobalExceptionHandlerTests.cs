using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using ThermoCity.Weather.Api.ExceptionHandling;
using ThermoCity.Weather.Application.Exceptions;
using Xunit;

namespace ThermoCity.Weather.UnitTests.ExceptionHandling;

public class GlobalExceptionHandlerTests
{
    private readonly Mock<IProblemDetailsService> _problemDetailsServiceMock = new();
    private readonly Mock<ILogger<GlobalExceptionHandler>> _loggerMock = new();
    private readonly GlobalExceptionHandler _handler;

    public GlobalExceptionHandlerTests()
    {
        _handler = new GlobalExceptionHandler(
            _problemDetailsServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldMapConflictException_To409Conflict()
    {
        var context = new DefaultHttpContext();
        var exception = new ConflictException("Usuário já existe");

        ProblemDetails? writtenProblem = null;
        _problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(ctx => writtenProblem = ctx.ProblemDetails)
            .ReturnsAsync(true);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.NotNull(writtenProblem);
        Assert.Equal(StatusCodes.Status409Conflict, writtenProblem.Status);
        Assert.Equal("Conflito", writtenProblem.Title);
        Assert.Equal("Usuário já existe", writtenProblem.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldMapInvalidInputException_To400BadRequest()
    {
        var context = new DefaultHttpContext();
        var exception = new InvalidInputException("Entrada inválida");

        ProblemDetails? writtenProblem = null;
        _problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(ctx => writtenProblem = ctx.ProblemDetails)
            .ReturnsAsync(true);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.NotNull(writtenProblem);
        Assert.Equal(StatusCodes.Status400BadRequest, writtenProblem.Status);
        Assert.Equal("Requisição inválida", writtenProblem.Title);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldMapMongoException_To503ServiceUnavailable()
    {
        var context = new DefaultHttpContext();
        var exception = new MongoConnectionException(new MongoDB.Driver.Core.Connections.ConnectionId(new MongoDB.Driver.Core.Servers.ServerId(new MongoDB.Driver.Core.Clusters.ClusterId(1), new System.Net.DnsEndPoint("localhost", 27017))), "timeout");

        ProblemDetails? writtenProblem = null;
        _problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(ctx => writtenProblem = ctx.ProblemDetails)
            .ReturnsAsync(true);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.NotNull(writtenProblem);
        Assert.Equal("Serviço indisponível", writtenProblem.Title);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldMapGenericException_To500InternalServerError()
    {
        var context = new DefaultHttpContext();
        var exception = new InvalidOperationException("Falha genérica");

        ProblemDetails? writtenProblem = null;
        _problemDetailsServiceMock
            .Setup(s => s.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Callback<ProblemDetailsContext>(ctx => writtenProblem = ctx.ProblemDetails)
            .ReturnsAsync(true);

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.NotNull(writtenProblem);
        Assert.Equal("Erro interno", writtenProblem.Title);
        Assert.Equal(GlobalExceptionHandler.MensagemErroInesperado, writtenProblem.Detail);
    }
}
