using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Domain.Entities;
using System.Net.Http.Json;

public class ApiNotificationPublisher : INotificationPublisher
{
    private readonly HttpClient _client;

    public ApiNotificationPublisher(HttpClient client) => _client = client;

    public Task NotifyJobUpdated(ReportJob job, CancellationToken ct)
    {
        var payload = new
        {
            jobId = job.Id,
            status = job.Status.ToString(),
            blobName = job.BlobName,
            error = job.Error
        };

        return _client.PostAsJsonAsync("/api/notifications/job-updated", payload, ct);
    }
}
