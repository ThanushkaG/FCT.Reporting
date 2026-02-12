using FCT.Reporting.Application.Reports.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FCT.Reporting.Api.Controllers
{
    [ApiController]
    [Route("api/report-jobs")]
    public class ReportJobsController : ControllerBase
    {
        private readonly ISender _mediator;

        public ReportJobsController(ISender mediator) => _mediator = mediator;

        [HttpPost]
        [Authorize(Policy = "Reports.Generate")]
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var id = await _mediator.Send(new CreateReportJobCommand(), ct);
            return AcceptedAtAction(nameof(Get), new { id }, new { id });
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "Reports.Read")]
        public IActionResult Get(Guid id)
        {
            return Ok(new { id });
        }
    }
}
