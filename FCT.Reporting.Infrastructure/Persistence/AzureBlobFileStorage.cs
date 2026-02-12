using Azure.Storage.Blobs;
using FCT.Reporting.Application.Abstractions;

namespace FCT.Reporting.Infrastructure.Persistence
{
    public sealed class AzureBlobFileStorage : IFileStorage
    {
        private readonly BlobServiceClient _client;
        private readonly string _containerName;

        public AzureBlobFileStorage(BlobServiceClient client, string containerName)
        {
            _client = client;
            _containerName = containerName;
        }

        public async Task EnsureAsync(CancellationToken ct)
        {
            var container = _client.GetBlobContainerClient(_containerName);
            await container.CreateIfNotExistsAsync(cancellationToken: ct);
        }

        public async Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken ct)
        {
            var container = _client.GetBlobContainerClient(_containerName);
            await container.CreateIfNotExistsAsync(cancellationToken: ct);
            var blob = container.GetBlobClient(blobName);
            content.Position = 0;
            await blob.UploadAsync(content, overwrite: true, cancellationToken: ct);
            return blob.Uri.ToString();
        }

        public Uri GetReadSas(string blobName, TimeSpan ttl)
        {
            // For simplicity return blob Uri; generating SAS requires account key or user delegation.
            var container = _client.GetBlobContainerClient(_containerName);
            var blob = container.GetBlobClient(blobName);
            return blob.Uri;
        }
    }
}
