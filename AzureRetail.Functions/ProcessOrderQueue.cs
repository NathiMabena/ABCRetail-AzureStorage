using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    public class ProcessOrderQueue
    {
        private readonly ILogger<ProcessOrderQueue> _logger;

        public ProcessOrderQueue(ILogger<ProcessOrderQueue> logger)
        {
            _logger = logger;
        }

        // The QueueTrigger attribute tells Azure to listen to your specific queue
        [Function("ProcessOrderQueue")]
        public async Task Run(
            [QueueTrigger("order-processing", Connection = "AzureStorageConnectionString")] string queueMessage)
        {
            _logger.LogInformation($"[START] Order Received from Queue: {queueMessage}");

            try
            {
                // 1. Fetch the connection string securely from local.settings.json
                string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString");

                // 2. Connect to your specific Azure File Share (Ensure "app-logs" matches your Azure setup)
                ShareClient shareClient = new ShareClient(connectionString, "app-logs");
                await shareClient.CreateIfNotExistsAsync();
                ShareDirectoryClient directoryClient = shareClient.GetRootDirectoryClient();

                // 3. Generate a unique digital receipt file name
                string fileName = $"receipt_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{queueMessage}.txt";
                ShareFileClient fileClient = directoryClient.GetFileClient(fileName);

                // 4. Create the text content for the receipt
                string fileContent = $"--- ABC RETAIL DIGITAL RECEIPT ---\n" +
                                     $"Order ID: {queueMessage}\n" +
                                     $"Processed At: {DateTime.UtcNow} UTC\n" +
                                     $"Status: FULFILLED via Serverless Architecture";

                // 5. Convert the text to a file stream and upload it to Azure Files
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
                await fileClient.CreateAsync(stream.Length);
                await fileClient.UploadRangeAsync(new Azure.HttpRange(0, stream.Length), stream);

                _logger.LogInformation($"[SUCCESS] Receipt {fileName} generated in Azure Files.");

                // 6. Connect to Azure Table Storage to update the order status
                TableClient tableClient = new TableClient(connectionString, "CustomerProduct");

                // Fetch the exact order using the PartitionKey and RowKey
                var tableResponse = await tableClient.GetEntityAsync<TableEntity>("Order", queueMessage);
                var orderEntity = tableResponse.Value;

                // Change the status property
                orderEntity["Status"] = "Processed";

                // Save the updated entity back to the cloud
                await tableClient.UpdateEntityAsync(orderEntity, orderEntity.ETag, TableUpdateMode.Replace);

                _logger.LogInformation($"[SUCCESS] Order {queueMessage} status updated to Processed in Table Storage.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Failed to process order {queueMessage}: {ex.Message}");
                throw; // Rethrowing tells the queue that the message failed so it can try again later
            }
        }
    }
}