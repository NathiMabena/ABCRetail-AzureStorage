using Microsoft.AspNetCore.Mvc;
using ABCRetail.AzureStorage.Models;
using ABCRetail.AzureStorage.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace ABCRetail.AzureStorage.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableStorageService _tableStorageService;

        // Add these two fields for our Serverless HTTP calls
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Update the constructor to inject all three tools
        public CustomersController(
            TableStorageService tableStorageService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _tableStorageService = tableStorageService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
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
        // THIS IS THE METHOD WE UPDATED TO USE SERVERLESS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerEntity entity)
        {
            if (!ModelState.IsValid)
            {
                return View(entity);
            }

            try
            {
                // 1. Get the base URL from appsettings.json
                string baseUrl = _configuration["AzureFunctionsBaseUrl"] ?? "http://localhost:7193/api/";

                // 2. Format the CustomerEntity data as a JSON payload
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(entity),
                    Encoding.UTF8,
                    "application/json");

                // 3. Send the HTTP POST request to your Azure Function
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsync($"{baseUrl}table/insert", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Serverless API failed to save the customer.");
                    return View(entity);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error connecting to Serverless API: {ex.Message}");
                return View(entity);
            }
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
                TempData["SuccessMessage"] = "Customer successfully deleted.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to delete customer. They might have already been removed or the database is unreachable.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}