using Azure;
using Azure.Data.Tables;

namespace ABCRetail.AzureStorage.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // RowKey of the CustomerEntity that placed this order
        public string CustomerId { get; set; } = string.Empty;

        // RowKey of the ProductEntity being ordered
        public string ProductId { get; set; } = string.Empty;

        public int Quantity { get; set; }

        // "Pending" until the background worker processes the queue message,
        // then "Processed"
        public string Status { get; set; } = "Pending";

        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
    }
}
