using ABCRetail.AzureStorage.Models;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.AzureStorage.Services
{
    // Generic across entity types (CustomerEntity, ProductEntity, OrderEntity),
    // since all three live in the same physical Azure Table, separated by PartitionKey.
    public class TableStorageService
    {
        private readonly TableClient _tableClient;

        public TableStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            var tableName = configuration["AzureStorage:TableName"];

            _tableClient = new TableClient(connectionString, tableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task AddEntityAsync<T>(T entity) where T : class, ITableEntity, new()
        {
            await _tableClient.AddEntityAsync(entity);
        }

        public async Task<List<T>> GetAllByPartitionAsync<T>(string partitionKey) where T : class, ITableEntity, new()
        {
            var results = new List<T>();
            await foreach (var entity in _tableClient.QueryAsync<T>(e => e.PartitionKey == partitionKey))
            {
                results.Add(entity);
            }
            return results;
        }

        public async Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                return response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return null;
            }
        }

        public async Task UpdateEntityAsync<T>(T entity) where T : class, ITableEntity, new()
        {
            await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
        }

        public async Task DeleteEntityAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // 1. Method to get all orders for the table view
        public async Task<List<OrderEntity>> GetAllOrdersAsync()
        {
            var orders = new List<OrderEntity>();

            // Grabs everything where the PartitionKey is "Order"
            var queryResults = _tableClient.QueryAsync<OrderEntity>(filter: $"PartitionKey eq 'Order'");

            await foreach (var entity in queryResults)
            {
                orders.Add(entity);
            }

            return orders;
        }

        // 2. Method to change "Pending" to "Processed"
        public async Task UpdateOrderStatusAsync(string orderId, string status)
        {
            try
            {
                // Fetch the exact order
                var response = await _tableClient.GetEntityAsync<OrderEntity>("Order", orderId);
                var order = response.Value;

                // Change the status
                order.Status = status;

                // Save the updated entity back to Azure
                await _tableClient.UpdateEntityAsync(order, order.ETag, Azure.Data.Tables.TableUpdateMode.Replace);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                Console.WriteLine($"Order {orderId} not found.");
            }
        }

        // 3. Method to add a new order to Table Storage
        public async Task AddOrderAsync(OrderEntity order)
        {
            try
            {
                await _tableClient.AddEntityAsync(order);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add order: {ex.Message}");
                throw; // Rethrow to let the controller handle the error message
            }
        }
    }
}