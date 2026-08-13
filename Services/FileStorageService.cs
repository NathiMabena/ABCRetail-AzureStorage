using Azure.Storage.Files.Shares;
using System.Text;

namespace ABCRetail.AzureStorage.Services
{
    public class FileStorageService
    {
        private readonly ShareClient _shareClient;

        public FileStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            var fileShareName = configuration["AzureStorage:FileShareName"];

            _shareClient = new ShareClient(connectionString, fileShareName);
            _shareClient.CreateIfNotExists();
        }

        public async Task WriteLogAsync(string logContent)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileName = $"log_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.txt";
            var fileClient = rootDir.GetFileClient(fileName);

            var bytes = Encoding.UTF8.GetBytes(logContent);
            using var stream = new MemoryStream(bytes);

            await fileClient.CreateAsync(bytes.Length);
            await fileClient.UploadAsync(stream);
        }

        public async Task<List<FileLogInfo>> ListLogsAsync()
        {
            var results = new List<FileLogInfo>();
            var rootDir = _shareClient.GetRootDirectoryClient();

            await foreach (var item in rootDir.GetFilesAndDirectoriesAsync())
            {
                if (!item.IsDirectory)
                {
                    results.Add(new FileLogInfo
                    {
                        Name = item.Name
                    });
                }
            }

            return results.OrderByDescending(f => f.Name).ToList();
        }

        public async Task<string> ReadLogAsync(string fileName)
        {
            var rootDir = _shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileName);

            var download = await fileClient.DownloadAsync();
            using var reader = new StreamReader(download.Value.Content);
            return await reader.ReadToEndAsync();
        }
    }

    public class FileLogInfo
    {
        public string Name { get; set; } = string.Empty;
    }
}
