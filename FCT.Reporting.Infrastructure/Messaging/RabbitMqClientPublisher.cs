using RabbitMQ.Client;
using System.Text;

namespace FCT.Reporting.Infrastructure.Messaging
{
    public interface IRabbitPublisher
    {
        Task PublishAsync(string messageType, string payloadJson, CancellationToken ct);
    }

    public sealed class RabbitMqClientPublisher(IConnectionFactory factory) : IRabbitPublisher, IDisposable
    {
        private readonly IConnection _connection = factory.CreateConnection();
        private const string ExchangeName = "fct.reporting.exchange";

        public Task PublishAsync(string messageType, string payloadJson, CancellationToken ct)
        {
            // IModel is not thread-safe → create per publish (safe)
            using var channel = _connection.CreateModel();

            channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false);
            channel.ConfirmSelect();

            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";
            props.Type = messageType;

            var body = System.Text.Encoding.UTF8.GetBytes(payloadJson);

            channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: messageType,
                basicProperties: props,
                body: body);

            if (!channel.WaitForConfirms(TimeSpan.FromSeconds(5)))
                throw new Exception("RabbitMQ publish confirm failed.");

            return Task.CompletedTask;
        }

        public void Dispose() => _connection.Dispose();
    }
}
