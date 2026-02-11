using FCT.Reporting.Domain.Enums;

namespace FCT.Reporting.Domain.Entities
{
    public sealed class ReportJob
    {
        public Guid Id { get; private set; }
        public string RequestedBy { get; private set; } = default!;
        public ReportJobStatus Status { get; private set; } = ReportJobStatus.Pending;

        public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; private set; } = DateTime.UtcNow;

        public string? BlobName { get; private set; }
        public string? Error { get; private set; }

        private ReportJob() { } // EF

        public ReportJob(Guid id, string requestedBy)
        {
            Id = id;
            RequestedBy = requestedBy;
            Status = ReportJobStatus.Pending;
            CreatedUtc = UpdatedUtc = DateTime.UtcNow;
        }

        public void MarkProcessing()
        {
            Status = ReportJobStatus.Processing;
            UpdatedUtc = DateTime.UtcNow;
        }

        public void MarkCompleted(string blobName)
        {
            Status = ReportJobStatus.Completed;
            BlobName = blobName;
            Error = null;
            UpdatedUtc = DateTime.UtcNow;
        }

        public void MarkFailed(string error)
        {
            Status = ReportJobStatus.Failed;
            Error = error;
            UpdatedUtc = DateTime.UtcNow;
        }
    }

}
