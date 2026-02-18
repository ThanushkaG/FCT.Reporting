using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FCT.Reporting.Infrastructure.Messaging
{
    public sealed class OutboxMessagePublisher : IMessagePublisher
    {
        private readonly IConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;

        public OutboxMessagePublisher(IConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task PublishAsync(string exchange, string routingKey, object message)
        {
            _connection ??= await _factory.CreateConnectionAsync();
            _channel ??= await _connection.CreateChannelAsync();

            await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                Persistent = true
            };

            await _channel.BasicPublishAsync(
                exchange,
                routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);
        }
    }
}
