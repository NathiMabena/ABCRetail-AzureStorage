using Microsoft.AspNetCore.Mvc;
using ABCRetail.AzureStorage.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;

namespace ABCRetail.AzureStorage.Controllers
{
    public class ImagesController : Controller
    {
        private readonly BlobStorageService _blobStorageService;

        // Add our HTTP tools
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ImagesController(
            BlobStorageService blobStorageService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _blobStorageService = blobStorageService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // GET: /Images
        public async Task<IActionResult> Index()
        {
            var blobs = await _blobStorageService.ListBlobsAsync();
            return View(blobs);
        }

        // POST: /Images/Upload 
        // THIS IS THE METHOD WE UPDATED TO USE SERVERLESS
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file selected." });
            }

            try
            {
                // 1. Get the base URL from appsettings.json (with our safety fallback)
                string baseUrl = _configuration["AzureFunctionsBaseUrl"] ?? "http://localhost:7193/api/";

                // 2. Open the file stream and prepare it for HTTP transmission
                using var stream = file.OpenReadStream();
                using var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

                // 3. Send the POST request to the Azure Function
                // Notice we append the filename in the URL so the Function knows what to call it!
                var client = _httpClientFactory.CreateClient();
                string requestUrl = $"{baseUrl}blob/upload?filename={Uri.EscapeDataString(file.FileName)}";

                var response = await client.PostAsync(requestUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    // Tell the frontend it worked and where to view the new image
                    return Json(new { success = true, url = $"/Images/View/{file.FileName}" });
                }
                else
                {
                    return Json(new { success = false, message = "Serverless API failed to upload the image." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error connecting to Serverless API: {ex.Message}" });
            }
        }

        // GET: /Images/View/{blobName} 
        public async Task<IActionResult> View(string blobName)
        {
            var result = await _blobStorageService.DownloadBlobAsync(blobName);
            if (result == null)
            {
                return NotFound();
            }
            return File(result.Value.Content, result.Value.ContentType);
        }

        // POST: /Images/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string blobName)
        {
            try
            {
                await _blobStorageService.DeleteBlobAsync(blobName);
                TempData["SuccessMessage"] = "Image successfully deleted.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Failed to delete image. It may have already been removed.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}