using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Contracts;
using FCT.Reporting.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FCT.Reporting.Application.Reports.Commands
{
    public sealed class CreateReportJobCommandHandler(
     IReportJobRepository repo,
     ICurrentUser currentUser,
     IMessagePublisher publisher,
     DbContext dbContext)
     : IRequestHandler<CreateReportJobCommand, Guid>
    {
        public async Task<Guid> Handle(CreateReportJobCommand request, CancellationToken ct)
        {
            await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

            var jobId = Guid.NewGuid();
            var job = new ReportJob(jobId, currentUser.UserId);

            await repo.AddAsync(job, ct);

            await publisher.PublishAsync(new GenerateReportRequested(jobId), ct);

            await repo.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return jobId;
        }
    }

}
