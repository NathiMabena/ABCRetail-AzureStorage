using Microsoft.AspNetCore.Mvc;
using ABCRetail.AzureStorage.Models;
using ABCRetail.AzureStorage.Services;


namespace ABCRetail.AzureStorage.Controllers
{
    public class DashboardController : Controller
    {
        private readonly TableStorageService _tableStorageService;

        public DashboardController(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        public async Task<IActionResult> Index()
        {
            var customers = await _tableStorageService.GetAllByPartitionAsync<CustomerEntity>("Customer");
            var products = await _tableStorageService.GetAllByPartitionAsync<ProductEntity>("Product");
            var orders = await _tableStorageService.GetAllByPartitionAsync<OrderEntity>("Order");

            var lowStockProducts = products.Where(p => p.StockQuantity <= 5).OrderBy(p => p.StockQuantity).ToList();

            var viewModel = new DashboardViewModel
            {
                TotalCustomers = customers.Count,
                TotalProducts = products.Count,
                TotalOrders = orders.Count,
                PendingOrders = orders.Count(o => o.Status == "Pending"),
                ProcessedOrders = orders.Count(o => o.Status == "Processed"),
                LowStockProducts = lowStockProducts,
                ProductNames = products.Select(p => p.Name).ToList(),
                ProductStockLevels = products.Select(p => p.StockQuantity).ToList()
            };

            return View(viewModel);
        }
    }
}