using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace FCT.Reporting.Infrastructure.Persistence
{
    public class ReportingDbContextFactory
    : IDesignTimeDbContextFactory<ReportingDbContext>
    {
        public ReportingDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration["Sql:ConnectionString"];

            var optionsBuilder = new DbContextOptionsBuilder<ReportingDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ReportingDbContext(optionsBuilder.Options);
        }
    }
}
