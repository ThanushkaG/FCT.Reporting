using Azure.Storage.Blobs;
using ClosedXML.Excel;
using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Domain.Entities;
using FCT.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FCT.Reporting.Worker
{
    public sealed class ReportWorker : BackgroundService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ReportingDbContext _db;
        private readonly INotificationPublisher _notifier;
        private readonly ILogger<ReportWorker> _logger;

        public ReportWorker(
            BlobServiceClient blobServiceClient,
            ReportingDbContext db,
            INotificationPublisher notifier,
            ILogger<ReportWorker> logger)
        {
            _blobServiceClient = blobServiceClient;
            _db = db;
            _notifier = notifier;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _db.ReportJobs
                        .Where(j => j.Status == Domain.Enums.ReportJobStatus.Pending)
                        .OrderBy(j => j.CreatedUtc)
                        .FirstOrDefaultAsync(stoppingToken);

                    if (job == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    job.MarkProcessing();
                    await _db.SaveChangesAsync(stoppingToken);

                    await ProcessJobAsync(job, stoppingToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker loop error");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }

        private async Task ProcessJobAsync(ReportJob job, CancellationToken ct)
        {
            if (job == null) return;

            try
            {
                await _notifier.NotifyJobUpdated(job, ct);

                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Report");
                ws.Cell(1, 1).Value = "Id";
                ws.Cell(1, 2).Value = "RequestedBy";
                ws.Cell(2, 1).Value = job.Id.ToString();
                ws.Cell(2, 2).Value = job.RequestedBy;

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                ms.Position = 0;

                var container = _blobServiceClient.GetBlobContainerClient("reports");
                await container.CreateIfNotExistsAsync(cancellationToken: ct);
                var blobName = $"reports/{job.Id}.xlsx";
                var blobClient = container.GetBlobClient(blobName);
                await blobClient.UploadAsync(ms, overwrite: true, cancellationToken: ct);

                job.MarkCompleted(blobName);
                await _db.SaveChangesAsync(ct);

                await _notifier.NotifyJobUpdated(job, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate report for job {JobId}", job.Id);
                job.MarkFailed(ex.Message);
                await _db.SaveChangesAsync(ct);
                await _notifier.NotifyJobUpdated(job, ct);
            }
        }
    }
}
