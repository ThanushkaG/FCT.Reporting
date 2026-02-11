using System.Text;
using System.Text.Json;
using FCT.Reporting.Application.Abstractions;
using RabbitMQ.Client;

namespace FCT.Reporting.Infrastructure.Messaging
{
    public sealed class RabbitMqPublisher : IMessagePublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        private const string ExchangeName = "reporting.exchange";

        public RabbitMqPublisher(IConnectionFactory factory)
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Enable publisher confirms (important for production)
            _channel.ConfirmSelect();
        }

        public Task PublishAsync<T>(T message, CancellationToken ct) where T : class
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var props = _channel.CreateBasicProperties();
            props.Persistent = true; // survive broker restart
            props.ContentType = "application/json";

            _channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: typeof(T).Name,
                basicProperties: props,
                body: body);

            // Confirm delivery to broker
            if (!_channel.WaitForConfirms(TimeSpan.FromSeconds(5)))
                throw new Exception("RabbitMQ publish not confirmed");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel.Close();
            _connection.Close();
        }
    }
}
