namespace FCT.Reporting.Infrastructure.Persistence
{
    public sealed class OutboxMessage
    {
        public Guid Id { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime? ProcessedUtc { get; set; }

        public string Type { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public int AttemptCount { get; set; }
        public string? LastError { get; set; }
    }
}
