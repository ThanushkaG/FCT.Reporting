namespace FCT.Reporting.Application.Abstractions
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message, CancellationToken ct) where T : class;
    }
}
