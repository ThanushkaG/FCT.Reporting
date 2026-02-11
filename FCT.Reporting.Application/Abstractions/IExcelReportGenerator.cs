namespace FCT.Reporting.Application.Abstractions
{
    public interface IExcelReportGenerator
    {
        Task<Stream> GenerateAsync(Guid jobId, CancellationToken ct);
    }
}
