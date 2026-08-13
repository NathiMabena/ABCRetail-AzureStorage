using Microsoft.AspNetCore.Mvc;
using ABCRetail.AzureStorage.Models;
using ABCRetail.AzureStorage.Services;

namespace ABCRetail.AzureStorage.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly BlobStorageService _blobStorageService;

        public ProductsController(TableStorageService tableStorageService, BlobStorageService blobStorageService)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
        }

        // GET: /Products
        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetAllByPartitionAsync<ProductEntity>("Product");
            return View(products);
        }

        // GET: /Products/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AvailableImages = await _blobStorageService.ListBlobsAsync();
            return View();
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductEntity entity)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableImages = await _blobStorageService.ListBlobsAsync();
                return View(entity);
            }

            await _tableStorageService.AddEntityAsync(entity);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Edit
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var entity = await _tableStorageService.GetEntityAsync<ProductEntity>(partitionKey, rowKey);
            if (entity == null)
            {
                return NotFound();
            }

            ViewBag.AvailableImages = await _blobStorageService.ListBlobsAsync();
            return View(entity);
        }

        // POST: /Products/Edit
        // Re-fetch the existing entity to get a valid ETag (see CustomersController for why).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductEntity entity)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableImages = await _blobStorageService.ListBlobsAsync();
                return View(entity);
            }

            var existing = await _tableStorageService.GetEntityAsync<ProductEntity>(entity.PartitionKey, entity.RowKey);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = entity.Name;
            existing.Price = entity.Price;
            existing.StockQuantity = entity.StockQuantity;
            existing.ImageBlobName = entity.ImageBlobName;

            await _tableStorageService.UpdateEntityAsync(existing);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Products/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            try
            {
                await _tableStorageService.DeleteEntityAsync(partitionKey, rowKey);
                TempData["SuccessMessage"] = "Product successfully deleted.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to delete product. It may have already been removed.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}