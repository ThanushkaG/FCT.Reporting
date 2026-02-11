namespace FCT.Reporting.Application.Abstractions
{
    public interface IFileStorage
    {
        Task EnsureAsync(CancellationToken ct);
        Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct);
        Uri GetReadSas(string blobName, TimeSpan ttl);
    }
}
