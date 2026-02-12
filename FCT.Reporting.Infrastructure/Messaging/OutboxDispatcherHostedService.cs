using FCT.Reporting.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FCT.Reporting.Infrastructure.Messaging
{
    public sealed class OutboxDispatcherHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxDispatcherHostedService> _logger;

        public OutboxDispatcherHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcherHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IRabbitPublisher>();

                    var batch = await db.OutboxMessages
                        .Where(x => x.ProcessedUtc == null)
                        .OrderBy(x => x.CreatedUtc)
                        .Take(25)
                        .ToListAsync(stoppingToken);

                    if (batch.Count == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    foreach (var msg in batch)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        try
                        {
                            await publisher.PublishAsync(msg.Type, msg.Payload, stoppingToken);
                            msg.ProcessedUtc = DateTime.UtcNow;
                            msg.LastError = null;
                        }
                        catch (Exception ex)
                        {
                            msg.AttemptCount++;
                            msg.LastError = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
                            _logger.LogError(ex, "Outbox publish failed for {Id} type {Type}", msg.Id, msg.Type);
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox dispatcher loop error");
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
        }
    }
}
