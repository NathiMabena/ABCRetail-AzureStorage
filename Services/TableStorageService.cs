using Azure;
using Azure.Data.Tables;
using ABCRetail.AzureStorage.Models;

namespace ABCRetail.AzureStorage.Services
{
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

        public async Task AddEntityAsync(CustomerProductEntity entity)
        {
            await _tableClient.AddEntityAsync(entity);
        }

        public async Task<List<CustomerProductEntity>> GetAllEntitiesAsync()
        {
            var results = new List<CustomerProductEntity>();
            await foreach (var entity in _tableClient.QueryAsync<CustomerProductEntity>())
            {
                results.Add(entity);
            }
            return results;
        }

        public async Task DeleteEntityAsync(string partitionKey, string rowKey)
        {
            await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }
    }
}
