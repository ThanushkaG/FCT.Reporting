using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Infrastructure.Persistence;
using System.Text.Json;

namespace FCT.Reporting.Infrastructure.Messaging
{
    public sealed class OutboxMessagePublisher : IMessagePublisher
    {
        private readonly ReportingDbContext _db;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public OutboxMessagePublisher(ReportingDbContext db) => _db = db;

        public Task PublishAsync<T>(T message, CancellationToken ct) where T : class
        {
            _db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                CreatedUtc = DateTime.UtcNow,
                Type = typeof(T).Name,
                Payload = JsonSerializer.Serialize(message, JsonOptions),
                AttemptCount = 0
            });

            return Task.CompletedTask;
        }
    }
}
