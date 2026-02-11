using FCT.Reporting.Application.Reports.Commands;
using FCT.Reporting.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCT.Reporting.Api.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController(IMediator mediator) : ControllerBase
    {
        [HttpPost]
        [Authorize(Policy = "Reports.Generate")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var jobId = await mediator.Send(new CreateReportJobCommand(), ct);
            return AcceptedAtAction(nameof(GetStatus), new { jobId }, new { jobId });
        }

        [HttpGet("{jobId:guid}")]
        [Authorize(Policy = "Reports.Read")]
        public async Task<IActionResult> GetStatus(Guid jobId, CancellationToken ct)
        {
            var dto = await mediator.Send(new GetReportJobQuery(jobId), ct);
            return dto is null ? NotFound() : Ok(dto);
        }
    }
}
