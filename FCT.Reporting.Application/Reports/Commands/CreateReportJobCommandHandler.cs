using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Contracts;
using FCT.Reporting.Domain.Entities;
using MediatR;

namespace FCT.Reporting.Application.Reports.Commands
{
    public sealed class CreateReportJobCommandHandler : IRequestHandler<CreateReportJobCommand, Guid>
    {
        private readonly IReportJobRepository _repo;
        private readonly ICurrentUser _currentUser;
        private readonly IMessagePublisher _publisher;

        public CreateReportJobCommandHandler(
            IReportJobRepository repo,
            ICurrentUser currentUser,
            IMessagePublisher publisher)
        {
            _repo = repo;
            _currentUser = currentUser;
            _publisher = publisher;
        }

        public async Task<Guid> Handle(CreateReportJobCommand request, CancellationToken ct)
        {
            var jobId = Guid.NewGuid();
            var job = new ReportJob(jobId, _currentUser.UserId);

            await _repo.AddAsync(job, ct);

            // write outbox message (publisher writes into Outbox table within same scope)
            await _publisher.PublishAsync(new GenerateReportRequested(jobId), ct);

            await _repo.SaveChangesAsync(ct);

            return jobId;
        }
    }
}
