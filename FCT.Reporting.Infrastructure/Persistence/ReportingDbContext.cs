using FCT.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCT.Reporting.Infrastructure.Persistence
{
    public class ReportingDbContext(DbContextOptions<ReportingDbContext> options) : DbContext(options)
    {
        public DbSet<ReportJob> ReportJobs => Set<ReportJob>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ReportJob>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.RequestedBy).HasMaxLength(256).IsRequired();
                b.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
                b.Property(x => x.BlobName).HasMaxLength(512);
                b.Property(x => x.Error).HasMaxLength(2000);
            });

            modelBuilder.Entity<OutboxMessage>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Type).HasMaxLength(256).IsRequired();
                b.Property(x => x.Payload).IsRequired();
                b.Property(x => x.LastError).HasMaxLength(2000);
                b.HasIndex(x => x.ProcessedUtc);
                b.HasIndex(x => x.CreatedUtc);
            });
        }
    }
}
