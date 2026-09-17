using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetail.Functions
{
    public class AddOrderToQueue
    {
        private readonly ILogger<AddOrderToQueue> _logger;

        public AddOrderToQueue(ILogger<AddOrderToQueue> logger)
        {
            _logger = logger;
        }

        [Function("AddOrderToQueue")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "queue/add")] HttpRequest req)
        {
            _logger.LogInformation("[START] Processing HTTP to Queue request.");

            try
            {
                // 1. Read the JSON payload to get the Order ID
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(requestBody);

                string orderId = data.TryGetProperty("OrderId", out var id) ? id.GetString() : $"ORDER-{Guid.NewGuid().ToString().Substring(0, 5)}";

                // 2. Connect to the Queue
                string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString");
                QueueClient queueClient = new QueueClient(connectionString, "order-processing");
                await queueClient.CreateIfNotExistsAsync();

                // 3. Convert the plain text Order ID to Base64, then send to the Queue
                byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(orderId);
                string base64Message = Convert.ToBase64String(messageBytes);
                await queueClient.SendMessageAsync(base64Message);

                _logger.LogInformation($"[SUCCESS] Order {orderId} added to Queue via HTTP.");

                return new OkObjectResult(new
                {
                    Status = "Success",
                    Message = "Order successfully added to the processing queue.",
                    OrderId = orderId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] Failed to add to queue: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}