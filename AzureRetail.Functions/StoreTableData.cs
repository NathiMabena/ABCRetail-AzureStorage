using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    public class StoreTableData
    {
        private readonly ILogger<StoreTableData> _logger;

        public StoreTableData(ILogger<StoreTableData> logger)
        {
            _logger = logger;
        }

        [Function("StoreTableData")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "table/insert")] HttpRequest req)
        {
            _logger.LogInformation("[START] Processing Table Insert request.");

            try
            {
                // 1. Read the JSON payload from the request body
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(requestBody);

                // 2. Extract partition key, row key, and dynamic fields
                string partitionKey = data.TryGetProperty("PartitionKey", out var pk) ? pk.GetString() : "General";
                string rowKey = data.TryGetProperty("RowKey", out var rk) ? rk.GetString() : Guid.NewGuid().ToString();

                // 3. Connect to Table Storage
                string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString");
                var serviceClient = new TableServiceClient(connectionString);
                var tableClient = serviceClient.GetTableClient("CustomerProduct");
                await tableClient.CreateIfNotExistsAsync();

                // 4. Build and save the TableEntity
                var entity = new TableEntity(partitionKey, rowKey);

                foreach (var prop in data.EnumerateObject())
                {
                    if (prop.Name != "PartitionKey" && prop.Name != "RowKey")
                    {
                        entity[prop.Name] = prop.Value.GetString();
                    }
                }

                await tableClient.AddEntityAsync(entity);

                _logger.LogInformation($"[SUCCESS] Saved entity {partitionKey} / {rowKey} to Table Storage.");

                return new OkObjectResult(new
                {
                    Status = "Success",
                    Message = "Record inserted into Azure Table successfully.",
                    PartitionKey = partitionKey,
                    RowKey = rowKey
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Failed to insert table data: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}