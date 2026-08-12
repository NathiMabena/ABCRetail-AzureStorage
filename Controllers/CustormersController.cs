using Microsoft.AspNetCore.Mvc;
using ABCRetail.AzureStorage.Models;
using ABCRetail.AzureStorage.Services;

namespace ABCRetail.AzureStorage.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableStorageService _tableStorageService;

        public CustomersController(TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        // GET: /Customers
        public async Task<IActionResult> Index()
        {
            var entities = await _tableStorageService.GetAllEntitiesAsync();
            return View(entities);
        }

        // GET: /Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerProductEntity entity)
        {
            if (!ModelState.IsValid)
            {
                return View(entity);
            }

            await _tableStorageService.AddEntityAsync(entity);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Customers/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteEntityAsync(partitionKey, rowKey);
            return RedirectToAction(nameof(Index));
        }
    }
}
