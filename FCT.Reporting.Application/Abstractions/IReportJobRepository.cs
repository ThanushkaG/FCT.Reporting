using FCT.Reporting.Domain.Entities;

namespace FCT.Reporting.Application.Abstractions
{
    public interface IReportJobRepository
    {
        Task AddAsync(ReportJob job, CancellationToken ct);
        Task<ReportJob?> GetAsync(Guid id, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
