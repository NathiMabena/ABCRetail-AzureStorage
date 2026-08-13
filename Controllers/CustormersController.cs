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
            var customers = await _tableStorageService.GetAllByPartitionAsync<CustomerEntity>("Customer");
            return View(customers);
        }

        // GET: /Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerEntity entity)
        {
            if (!ModelState.IsValid)
            {
                return View(entity);
            }

            await _tableStorageService.AddEntityAsync(entity);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Customers/Edit
        public async Task<IActionResult> Edit(string partitionKey, string rowKey)
        {
            var entity = await _tableStorageService.GetEntityAsync<CustomerEntity>(partitionKey, rowKey);
            if (entity == null)
            {
                return NotFound();
            }
            return View(entity);
        }

        // POST: /Customers/Edit
        // Note: we re-fetch the existing entity server-side rather than trusting the
        // posted ETag, since Azure.ETag doesn't round-trip cleanly through MVC model
        // binding from a hidden form field. Re-fetching guarantees a valid ETag.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerEntity entity)
        {
            if (!ModelState.IsValid)
            {
                return View(entity);
            }

            var existing = await _tableStorageService.GetEntityAsync<CustomerEntity>(entity.PartitionKey, entity.RowKey);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Name = entity.Name;
            existing.Email = entity.Email;
            existing.Phone = entity.Phone;

            await _tableStorageService.UpdateEntityAsync(existing);
            return RedirectToAction(nameof(Index));
        }
        // POST: /Customers/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            try
            {
                await _tableStorageService.DeleteEntityAsync(partitionKey, rowKey);

                // Pass a success message to the view
                TempData["SuccessMessage"] = "Customer successfully deleted.";
            }
            catch (Exception)
            {
                // If Azure throws an error (e.g., entity doesn't exist or network drop), catch it
                TempData["ErrorMessage"] = "Failed to delete customer. They might have already been removed or the database is unreachable.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}