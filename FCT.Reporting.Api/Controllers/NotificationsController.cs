using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FCT.Reporting.Api.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationPublisher _publisher;

        public NotificationsController(INotificationPublisher publisher) => _publisher = publisher;

        [HttpPost("job-updated")]
        public async Task<IActionResult> JobUpdated([FromBody] ReportJobDto dto, CancellationToken ct)
        {
            // lightweight mapping - only fields used by notification
            var job = new ReportJob(dto.Id, dto.RequestedBy);
            // set status/optional blob/error
            if (!string.IsNullOrEmpty(dto.BlobName)) job.MarkCompleted(dto.BlobName);
            if (!string.IsNullOrEmpty(dto.Error)) job.MarkFailed(dto.Error);

            await _publisher.NotifyJobUpdated(job, ct);
            return NoContent();
        }
    }

    public record ReportJobDto(Guid Id, string RequestedBy, string? BlobName, string? Error);
}
