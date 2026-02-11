using MediatR;

namespace FCT.Reporting.Application.Reports.Commands
{
    public record CreateReportJobCommand() : IRequest<Guid>;
}
