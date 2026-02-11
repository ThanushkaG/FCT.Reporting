
using MediatR;

namespace FCT.Reporting.Application.Reports.Queries
{
    public record ReportJobDto(Guid Id, string Status, DateTime CreatedUtc, DateTime UpdatedUtc, string? Error);

    public record GetReportJobQuery(Guid JobId) : IRequest<ReportJobDto?>;
}
