using Microsoft.AspNetCore.Mvc;
using ABCRetail.AzureStorage.Services;

namespace ABCRetail.AzureStorage.Controllers
{
    public class ImagesController : Controller
    {
        private readonly BlobStorageService _blobStorageService;

        public ImagesController(BlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        // GET: /Images
        public async Task<IActionResult> Index()
        {
            var blobs = await _blobStorageService.ListBlobsAsync();
            return View(blobs);
        }

        // POST: /Images/Upload 
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file selected." });
            }

            using var stream = file.OpenReadStream();
            var url = await _blobStorageService.UploadBlobAsync(stream, file.FileName, file.ContentType);

            return Json(new { success = true, url });
        }

        // GET: /Images/View/{blobName} 
        public async Task<IActionResult> View(string blobName)
        {
            var result = await _blobStorageService.DownloadBlobAsync(blobName);
            if (result == null)
            {
                return NotFound(); // Safely handle missing images without crashing
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