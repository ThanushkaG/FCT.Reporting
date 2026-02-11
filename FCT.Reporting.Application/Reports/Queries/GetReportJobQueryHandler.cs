using FCT.Reporting.Application.Abstractions;
using MediatR;

namespace FCT.Reporting.Application.Reports.Queries
{
    public class GetReportJobQueryHandler(IReportJobRepository repo) : IRequestHandler<GetReportJobQuery, ReportJobDto?>
    {
        public async Task<ReportJobDto?> Handle(GetReportJobQuery request, CancellationToken ct)
        {
            var job = await repo.GetAsync(request.JobId, ct);
            return job is null
                ? null
                : new ReportJobDto(job.Id, job.Status.ToString(), job.CreatedUtc, job.UpdatedUtc, job.Error);
        }
    }
}
