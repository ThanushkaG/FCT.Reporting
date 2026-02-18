using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Contracts;
using FCT.Reporting.Domain.Entities;
using MediatR;

namespace FCT.Reporting.Application.Reports.Commands
{
    public sealed class CreateReportJobCommandHandler
    : IRequestHandler<CreateReportJobCommand, Guid>
    {
        private readonly IReportJobRepository _repository;
        private readonly IMessagePublisher _rabbitPublisher;

        public CreateReportJobCommandHandler(
            IReportJobRepository repository,
            IMessagePublisher rabbitPublisher)
        {
            _repository = repository;
            _rabbitPublisher = rabbitPublisher;
        }

        public async Task<Guid> Handle(
            CreateReportJobCommand request,
            CancellationToken ct)
        {
            var job = new ReportJob(Guid.NewGuid(), request.RequestedBy);

            await _repository.AddAsync(job, ct);
            await _repository.SaveChangesAsync(ct);

            await _rabbitPublisher.PublishAsync(
                "report.exchange",
                "report.requested",
                new { JobId = job.Id });

            return job.Id;
        }
    }
}
