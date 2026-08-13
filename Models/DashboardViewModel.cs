namespace ABCRetail.AzureStorage.Models
{
    public class DashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessedOrders { get; set; }
        public List<ProductEntity> LowStockProducts { get; set; } = new();

        // Flat lists for feeding directly into a Chart.js bar chart
        public List<string> ProductNames { get; set; } = new();
        public List<int> ProductStockLevels { get; set; } = new();
    }
}
