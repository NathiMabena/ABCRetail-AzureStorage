using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using ABCRetail.AzureStorage.Models;
using ABCRetail.AzureStorage.Services;

namespace ABCRetail.AzureStorage.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TableStorageService _tableService;
        private readonly QueueStorageService _queueService;
        private readonly FileStorageService _fileService;

        public OrdersController(
            TableStorageService tableService,
            QueueStorageService queueService,
            FileStorageService fileService)
        {
            _tableService = tableService;
            _queueService = queueService;
            _fileService = fileService;
        }

        // --- 1. INDEX PAGE ---
        public async Task<IActionResult> Index()
        {
            var orders = await _tableService.GetAllOrdersAsync();

            // Updates the Queue badge on your UI based on your actual method
            ViewBag.QueueCount = await _queueService.GetApproximateMessageCountAsync();

            return View(orders);
        }

        // --- 2. CREATE PAGE ---
        public async Task<IActionResult> Create()
        {
            // Fetch your existing customers and products from Table Storage
            var customers = await _tableService.GetAllByPartitionAsync<CustomerEntity>("Customer");
            var products = await _tableService.GetAllByPartitionAsync<ProductEntity>("Product");

            // Pass them to the View so the dropdowns can see them
            ViewBag.Customers = customers;
            ViewBag.Products = products;

            return View();
        }

        // --- 3. SUBMIT ORDER ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderEntity newOrder)
        {
            try
            {
                newOrder.RowKey = Guid.NewGuid().ToString();
                newOrder.PartitionKey = "Order";
                newOrder.Status = "Pending";
                newOrder.Timestamp = DateTime.UtcNow;

                // Save to Table Storage
                await _tableService.AddOrderAsync(newOrder); // Ensure this method exists in your TableService

                // Send to Queue Storage
                await _queueService.SendMessageAsync(newOrder.RowKey);

                TempData["SuccessMessage"] = "Order placed successfully! It is now pending in the queue.";
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

                // Grab the first message using your specific method
                var message = await _queueService.ReceiveMessageAsync();

                if (message == null)
                {
                    TempData["SuccessMessage"] = "The queue is already empty. No pending orders to process.";
                    return RedirectToAction(nameof(Index));
                }

                // Loop as long as there are messages in the queue
                while (message != null)
                {
                    string orderId = message.MessageText;

                    // 1. Update Table Storage
                    await _tableService.UpdateOrderStatusAsync(orderId, "Processed");

                    // 2. Write the log using your exact method
                    string logText = $"[{DateTime.UtcNow}] Processed Order ID {orderId} successfully.";
                    await _fileService.WriteLogAsync(logText);

                    // 3. Delete the message from the queue
                    await _queueService.DeleteMessageAsync(message.MessageId, message.PopReceipt);

                    processedCount++;

                    // Check for the next message
                    message = await _queueService.ReceiveMessageAsync();
                }

                TempData["SuccessMessage"] = $"Success! {processedCount} pending orders were processed and logged to Azure Files.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while processing the queue: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}