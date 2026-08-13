using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ABCRetail.AzureStorage.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            var containerName = configuration["AzureStorage:ContainerName"];

            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        public async Task<string> UploadBlobAsync(Stream fileStream, string fileName, string contentType)
        {
            // Prefix with a GUID to avoid filename collisions
            var blobName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = _containerClient.GetBlobClient(blobName);

            var headers = new BlobHttpHeaders { ContentType = contentType };
            await blobClient.UploadAsync(fileStream, new BlobUploadOptions { HttpHeaders = headers });

            return blobClient.Uri.ToString();
        }

        public async Task<List<BlobItemInfo>> ListBlobsAsync()
        {
            var results = new List<BlobItemInfo>();

            await foreach (var blob in _containerClient.GetBlobsAsync())
            {
                var blobClient = _containerClient.GetBlobClient(blob.Name);
                results.Add(new BlobItemInfo
                {
                    Name = blob.Name,
                    Url = blobClient.Uri.ToString(),
                    LastModified = blob.Properties.LastModified?.DateTime
                });
            }

            return results;
        }

        public async Task DeleteBlobAsync(string blobName)
        {
            await _containerClient.DeleteBlobIfExistsAsync(blobName);
        }

        public async Task<(Stream Content, string ContentType)?> DownloadBlobAsync(string blobName)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                if (!await blobClient.ExistsAsync()) return null;

                var download = await blobClient.DownloadContentAsync();
                var contentType = download.Value.Details.ContentType ?? "application/octet-stream";
                var stream = download.Value.Content.ToStream();
                return (stream, contentType);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }
    }

    public class BlobItemInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime? LastModified { get; set; }
    }
}