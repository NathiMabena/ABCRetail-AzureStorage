using Azure;
using Azure.Data.Tables;

namespace ABCRetail.AzureStorage.Models
{
    public class CustomerProductEntity : ITableEntity
    {
        //Azure Table Storage 
        public string PartitionKey { get; set; } = "CustomerProduct";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public double Price { get; set; }
    }
}
