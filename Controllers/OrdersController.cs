using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ABCRetail.AzureStorage.Models;
using ABCRetail.AzureStorage.Services;

namespace ABCRetail.AzureStorage.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TableStorageService _tableService;
        private readonly QueueStorageService _queueService;
        private readonly FileStorageService _fileService;

        // Add our Serverless HTTP tools
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public OrdersController(
            TableStorageService tableService,
            QueueStorageService queueService,
            FileStorageService fileService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _tableService = tableService;
            _queueService = queueService;
            _fileService = fileService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // --- 1. INDEX PAGE ---
        public async Task<IActionResult> Index()
        {
            var orders = await _tableService.GetAllOrdersAsync();
            ViewBag.QueueCount = await _queueService.GetApproximateMessageCountAsync();
            return View(orders);
        }

        // --- 2. CREATE PAGE ---
        public async Task<IActionResult> Create()
        {
            var customers = await _tableService.GetAllByPartitionAsync<CustomerEntity>("Customer");
            var products = await _tableService.GetAllByPartitionAsync<ProductEntity>("Product");

            ViewBag.Customers = customers;
            ViewBag.Products = products;

            return View();
        }

        // --- 3. SUBMIT ORDER (UPDATED FOR SERVERLESS & MAPPING) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderEntity newOrder, string CustomerRowKey, string ProductRowKey)
        {
            try
            {
                // Map the form's explicit input names to your model properties
                newOrder.CustomerId = CustomerRowKey;
                newOrder.ProductId = ProductRowKey;

                newOrder.RowKey = Guid.NewGuid().ToString();
                newOrder.PartitionKey = "Order";
                newOrder.Status = "Pending";

                // Convert UTC server time to South Africa Standard Time (SAST, UTC+2)
                TimeZoneInfo sastZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
                newOrder.Timestamp = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, sastZone);

                // 1. Save the visual record to Table Storage (so it shows on your UI with proper IDs and local time)
                await _tableService.AddOrderAsync(newOrder);

                // 2. Get the base URL from appsettings.json
                string baseUrl = _configuration["AzureFunctionsBaseUrl"] ?? "http://localhost:7193/api/";

                // 3. Create the JSON payload specifically for our Function {"OrderId": "..."}
                var payload = new { OrderId = newOrder.RowKey };
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                // 4. Send the POST request to the Azure Function
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync($"{baseUrl}queue/add", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Order placed! The Serverless background worker is generating your receipt.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Order saved, but the Serverless function failed to queue it.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error placing order: {ex.Message}";
                return View(newOrder);
            }
        }

        // --- 4. MANUAL BATCH PROCESSOR ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPendingOrders()
        {
            try
            {
                int processedCount = 0;
                var message = await _queueService.ReceiveMessageAsync();

                if (message == null)
                {
                    TempData["SuccessMessage"] = "The queue is already empty. The background function likely processed everything instantly!";
                    return RedirectToAction(nameof(Index));
                }

                while (message != null)
                {
                    string orderId = message.MessageText;
                    await _tableService.UpdateOrderStatusAsync(orderId, "Processed");

                    TimeZoneInfo sastZone = TimeZoneInfo.FindSystemTimeZoneById("South Africa Standard Time");
                    DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, sastZone);

                    string logText = $"[{localTime}] Processed Order ID {orderId} successfully.";
                    await _fileService.WriteLogAsync(logText);

                    await _queueService.DeleteMessageAsync(message.MessageId, message.PopReceipt);
                    processedCount++;

                    message = await _queueService.ReceiveMessageAsync();
                }

                TempData["SuccessMessage"] = $"Manually processed {processedCount} pending orders.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}