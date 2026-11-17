
using System.Text;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using ThermoCity.Weather.Application.Interfaces;

namespace ThermoCity.Weather.Infrastructure.RabbitMQ;

public class RabbitMqHeatAlertPublisher : IHeatAlertPublisher
{
    private readonly string _connectionUri;
    private readonly string _queueName;

    public RabbitMqHeatAlertPublisher(IConfiguration configuration)
    {
        _connectionUri = configuration["RabbitMQ:Uri"]
            ?? throw new InvalidOperationException(
                "RabbitMQ:Uri não foi configurado.");

        _queueName = configuration["RabbitMQ:Queue"]
            ?? throw new InvalidOperationException(
                "RabbitMQ:Queue não foi configurado.");
    }

    public async Task PublishAsync(
        string message,
        CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_connectionUri)
        };

        await using var connection =
            await factory.CreateConnectionAsync(cancellationToken);

        await using var channel =
            await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _queueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
