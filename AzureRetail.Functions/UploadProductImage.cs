using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    public class UploadProductImage
    {
        private readonly ILogger<UploadProductImage> _logger;

        public UploadProductImage(ILogger<UploadProductImage> logger)
        {
            _logger = logger;
        }

        [Function("UploadProductImage")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "blob/upload")] HttpRequest req)
        {
            _logger.LogInformation("[START] Processing Blob Upload request.");

            try
            {
                // 1. Get the requested filename from the URL, or generate a random one
                string fileName = req.Query["filename"];
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"product_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg";
                }

                // 2. Connect to Blob Storage
                string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString");
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

                // Ensure this matches your container name exactly (lowercase)
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("product-images");
                await containerClient.CreateIfNotExistsAsync();

                // 3. Get a reference to the new file and upload the raw request body
                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                await blobClient.UploadAsync(req.Body, overwrite: true);

                _logger.LogInformation($"[SUCCESS] Uploaded {fileName} to Blob Storage.");

                return new OkObjectResult(new
                {
                    Status = "Success",
                    Message = "Image uploaded to Blob Storage successfully.",
                    FileName = fileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Failed to upload blob: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}