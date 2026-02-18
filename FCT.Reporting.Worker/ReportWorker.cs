using Azure.Storage.Blobs;
using ClosedXML.Excel;
using FCT.Reporting.Application.Abstractions;
using FCT.Reporting.Domain.Entities;
using FCT.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace FCT.Reporting.Worker
{
    public sealed class ReportWorker : BackgroundService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ReportingDbContext _db;
        private readonly INotificationPublisher _notifier;
        private readonly ILogger<ReportWorker> _logger;
        private readonly IConnectionFactory _factory;

        public ReportWorker(
    IConnectionFactory factory,
    BlobServiceClient blobServiceClient,
    ReportingDbContext db,
    INotificationPublisher notifier,
    ILogger<ReportWorker> logger)
        {
            _factory = factory;
            _blobServiceClient = blobServiceClient;
            _db = db;
            _notifier = notifier;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReportWorker started (RabbitMQ consumer)");

            var connection = await _factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync("report.exchange", ExchangeType.Topic, durable: true);
            await channel.QueueDeclareAsync("report.queue", durable: true, exclusive: false);
            await channel.QueueBindAsync("report.queue", "report.exchange", "report.requested");

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    var body = Encoding.UTF8.GetString(ea.Body.ToArray());

                    var payload = JsonSerializer.Deserialize<Dictionary<string, Guid>>(body);

                    if (payload == null || !payload.TryGetValue("JobId", out var jobId))
                    {
                        await channel.BasicNackAsync(ea.DeliveryTag, false, false);
                        return;
                    }

                    var job = await _db.ReportJobs
                        .FirstOrDefaultAsync(j => j.Id == jobId, stoppingToken);

                    if (job == null)
                    {
                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    job.MarkProcessing();
                    await _db.SaveChangesAsync(stoppingToken);

                    await ProcessJobAsync(job, stoppingToken);

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing RabbitMQ message");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await channel.BasicConsumeAsync("report.queue", autoAck: false, consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
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

                // Blob name = GUID only
                var blobName = $"{job.Id}.xlsx";
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
