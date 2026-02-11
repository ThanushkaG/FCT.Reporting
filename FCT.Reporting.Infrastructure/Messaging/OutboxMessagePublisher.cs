using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FCT.Reporting.Infrastructure.Messaging
{
    public sealed class OutboxMessagePublisher(ReportingDbContext db) : IMessagePublisher
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public Task PublishAsync<T>(T message, CancellationToken ct) where T : class
        {
            db.OutboxMessages.Add(new OutboxMessage
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
