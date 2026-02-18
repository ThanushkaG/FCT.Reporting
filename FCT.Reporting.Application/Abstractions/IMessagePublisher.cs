namespace FCT.Reporting.Application.Abstractions
{
    public interface IMessagePublisher
    {
        Task PublishAsync(string exchange, string routingKey, object message);
    }
}
