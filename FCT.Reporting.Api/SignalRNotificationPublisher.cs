using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using FCT.Reporting.Api.Hubs;

namespace FCT.Reporting.Api
{
    public class SignalRNotificationPublisher : INotificationPublisher
    {
        private readonly IHubContext<ReportsHub> _hub;

        public SignalRNotificationPublisher(IHubContext<ReportsHub> hub) => _hub = hub;

        public Task NotifyJobUpdated(ReportJob job, CancellationToken ct)
        {
            var payload = new
            {
                jobId = job.Id,
                status = job.Status.ToString(),
                blobName = job.BlobName,
                error = job.Error
            };

            return _hub.Clients.All.SendAsync("JobUpdated", payload, ct);
        }
    }
}
