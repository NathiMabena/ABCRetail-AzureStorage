using Microsoft.AspNetCore.Mvc;
using ABCRetail.AzureStorage.Services;

namespace ABCRetail.AzureStorage.Controllers
{
    public class LogsController : Controller
    {
        private readonly FileStorageService _fileStorageService;

        public LogsController(FileStorageService fileStorageService)
        {
            _fileStorageService = fileStorageService;
        }

        // GET: /Logs
        public async Task<IActionResult> Index()
        {
            var logs = await _fileStorageService.ListLogsAsync();
            return View(logs);
        }

        // GET: /Logs/View?fileName=...
        public async Task<IActionResult> View(string fileName)
        {
            var content = await _fileStorageService.ReadLogAsync(fileName);
            return Content(content, "text/plain");
        }
    }
}