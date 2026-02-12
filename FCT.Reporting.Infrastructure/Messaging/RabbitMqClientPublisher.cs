using RabbitMQ.Client;
using System.Text;

namespace FCT.Reporting.Infrastructure.Messaging;

public interface IRabbitPublisher
{
    Task PublishAsync(string messageType, string payloadJson, CancellationToken ct);
}

public sealed class RabbitMqClientPublisher : IRabbitPublisher, IAsyncDisposable
{
    private readonly IConnectionFactory _factory;
    private IConnection? _connection;

    private const string ExchangeName = "fct.reporting.exchange";

    public RabbitMqClientPublisher(IConnectionFactory factory)
    {
        _factory = factory;
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        _connection = await _factory.CreateConnectionAsync(ct);
        return _connection;
    }

    public async Task PublishAsync(string messageType, string payloadJson, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(ct);

        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: ct);

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = messageType
        };

        var body = Encoding.UTF8.GetBytes(payloadJson);

        try
        {
            await channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: messageType,
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: ct);
        }
        catch (Exception)
        {
            // retry strategy can be added here
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
            await _connection.DisposeAsync();
    }
}
