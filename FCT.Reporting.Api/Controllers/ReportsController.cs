using Azure.Storage.Blobs;
using FCT.Reporting.Application.Reports.Commands;
using FCT.Reporting.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCT.Reporting.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly BlobServiceClient _blobServiceClient;

        public ReportsController(
            IMediator mediator,
            BlobServiceClient blobServiceClient)
        {
            _mediator = mediator;
            _blobServiceClient = blobServiceClient;
        }

        [HttpPost]
        [Authorize(Policy = "Reports.Generate")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var requestedBy = User.Identity?.Name ?? "system";
            var jobId = await _mediator.Send(new CreateReportJobCommand(requestedBy), ct);
            return AcceptedAtAction(nameof(GetStatus), new { jobId }, new { jobId });
        }

        [HttpGet("{jobId:guid}")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<IActionResult> GetStatus(Guid jobId, CancellationToken ct)
        {
            var dto = await _mediator.Send(new GetReportJobQuery(jobId), ct);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpGet("{jobId:guid}/download")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<IActionResult> Download(Guid jobId, CancellationToken ct)
        {
            var dto = await _mediator.Send(
                new GetReportJobQuery(jobId), ct);

            if (dto is null || dto.Status != "Completed")
                return NotFound();

            var container = _blobServiceClient.GetBlobContainerClient("reports");

            var blobClient = container.GetBlobClient($"{jobId}.xlsx");

            if (!await blobClient.ExistsAsync(ct))
                return NotFound();

            var stream = await blobClient.OpenReadAsync(cancellationToken: ct);

            return File(
                stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{jobId}.xlsx");
        }
    }
}

