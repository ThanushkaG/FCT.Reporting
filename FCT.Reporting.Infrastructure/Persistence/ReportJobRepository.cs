using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCT.Reporting.Infrastructure.Persistence
{
    public class ReportJobRepository(ReportingDbContext db) : IReportJobRepository
    {
        public Task AddAsync(ReportJob job, CancellationToken ct) => db.ReportJobs.AddAsync(job, ct).AsTask();

        public Task<ReportJob?> GetAsync(Guid id, CancellationToken ct) =>
            db.ReportJobs.SingleOrDefaultAsync(x => x.Id == id, ct);

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
