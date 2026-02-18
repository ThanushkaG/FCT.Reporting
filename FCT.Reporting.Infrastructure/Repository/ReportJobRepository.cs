using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Domain.Entities;
using FCT.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCT.Reporting.Infrastructure.Repository
{
    public sealed class ReportJobRepository : IReportJobRepository
    {
        private readonly ReportingDbContext _db;

        public ReportJobRepository(ReportingDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(ReportJob job, CancellationToken ct)
            => await _db.ReportJobs.AddAsync(job, ct);

        public Task<ReportJob?> GetAsync(Guid id, CancellationToken ct) =>
           _db.ReportJobs.SingleOrDefaultAsync(x => x.Id == id, ct);


        public async Task<ReportJob?> GetByIdAsync(Guid id, CancellationToken ct)
            => await _db.ReportJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

        public Task SaveChangesAsync(CancellationToken ct)
            => _db.SaveChangesAsync(ct);
    }
}
