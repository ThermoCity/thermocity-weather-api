using ThermoCity.Weather.Infrastructure.Mongo;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

//Configuração dos Controllers
builder.Services.AddControllers();

//Configuração do MongoDB
//Registra o cliente, o banco de dados e o Health Check do MongoDB.
builder.Services.AddMongoDb(builder.Configuration);

//Health Check
//Verifica se a própria aplicação está em execução.
builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" }
    );

//Configuração do Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//Configuração do Swagger
//Disponibiliza a documentação interativa apenas no ambiente de desenvolvimento.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//Configuração dos Controllers
app.MapControllers();

//Configura o endpoint de Liveness (Vivacidade)
//Verifica se a aplicação está funcionando, sem consultar dependências externas.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

//Configura o endpoint de Readiness (Prontidão)
//Verifica se as dependências necessárias, como o MongoDB, estão disponíveis.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

//Permite utilizar WebApplicationFactory nos testes de integração.
public partial class Program
{
}
