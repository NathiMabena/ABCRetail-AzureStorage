using Microsoft.AspNetCore.Mvc;
using ABCRetail.AzureStorage.Models;
using ABCRetail.AzureStorage.Services;

namespace ABCRetail.AzureStorage.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileStorageService _fileStorageService;

        public OrdersController(
            TableStorageService tableStorageService,
            QueueStorageService queueStorageService,
            FileStorageService fileStorageService)
        {
            _tableStorageService = tableStorageService;
            _queueStorageService = queueStorageService;
            _fileStorageService = fileStorageService;
        }

        // GET: /Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _tableStorageService.GetAllByPartitionAsync<OrderEntity>("Order");
            var customers = await _tableStorageService.GetAllByPartitionAsync<CustomerEntity>("Customer");
            var products = await _tableStorageService.GetAllByPartitionAsync<ProductEntity>("Product");

            var customerLookup = customers.ToDictionary(c => c.RowKey, c => c.Name);
            var productLookup = products.ToDictionary(p => p.RowKey, p => p.Name);

            var viewModels = orders
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderViewModel
                {
                    PartitionKey = o.PartitionKey,
                    RowKey = o.RowKey,
                    CustomerName = customerLookup.GetValueOrDefault(o.CustomerId, "(unknown customer)"),
                    ProductName = productLookup.GetValueOrDefault(o.ProductId, "(unknown product)"),
                    Quantity = o.Quantity,
                    Status = o.Status,
                    OrderDate = o.OrderDate
                })
                .ToList();

            ViewBag.QueueCount = await _queueStorageService.GetApproximateMessageCountAsync();
            return View(viewModels);
        }

        // GET: /Orders/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _tableStorageService.GetAllByPartitionAsync<CustomerEntity>("Customer");
            ViewBag.Products = await _tableStorageService.GetAllByPartitionAsync<ProductEntity>("Product");
            return View();
        }

        // POST: /Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string customerId, string productId, int quantity)
        {
            if (string.IsNullOrEmpty(customerId) || string.IsNullOrEmpty(productId) || quantity <= 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a customer, a product, and a valid quantity.");
                ViewBag.Customers = await _tableStorageService.GetAllByPartitionAsync<CustomerEntity>("Customer");
                ViewBag.Products = await _tableStorageService.GetAllByPartitionAsync<ProductEntity>("Product");
                return View();
            }

            var order = new OrderEntity
            {
                CustomerId = customerId,
                ProductId = productId,
                Quantity = quantity,
                Status = "Pending",
                OrderDate = DateTimeOffset.UtcNow
            };

            // Persist the order first so it exists when the worker looks it up
            await _tableStorageService.AddEntityAsync(order);

            // Send just the Order's RowKey — the worker looks up the full order from Table Storage
            await _queueStorageService.SendMessageAsync(order.RowKey);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Orders/ViewLog?fileName=...
        public async Task<IActionResult> ViewLog(string fileName)
        {
            var content = await _fileStorageService.ReadLogAsync(fileName);
            return Content(content, "text/plain");
        }
    }
}