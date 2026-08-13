namespace ABCRetail.AzureStorage.Models
{
    // Read-only shape for displaying an order with resolved Customer/Product names,
    // instead of showing raw RowKey GUIDs in the UI.
    public class OrderViewModel
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset OrderDate { get; set; }
    }
}
