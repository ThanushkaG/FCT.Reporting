using FCT.Reporting.Domain.Entities;

namespace FCT.Reporting.Application.Abstractions
{
    public interface INotificationPublisher
    {
        Task NotifyJobUpdated(ReportJob job, CancellationToken ct);
    }
}
