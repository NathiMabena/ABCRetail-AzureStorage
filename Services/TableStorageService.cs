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
    }
}