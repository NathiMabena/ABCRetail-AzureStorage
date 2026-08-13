using Azure.Storage.Queues;

namespace ABCRetail.AzureStorage.Services
{
    public class QueueStorageService
    {
        private readonly QueueClient _queueClient;

        public QueueStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            var queueName = configuration["AzureStorage:QueueName"];

            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        public async Task SendMessageAsync(string message)
        {
            await _queueClient.SendMessageAsync(message);
        }

        // Used by the background worker to pick up and remove messages
        public async Task<Azure.Storage.Queues.Models.QueueMessage?> ReceiveMessageAsync()
        {
            var response = await _queueClient.ReceiveMessageAsync(TimeSpan.FromSeconds(30));
            return response.Value;
        }

        public async Task DeleteMessageAsync(string messageId, string popReceipt)
        {
            await _queueClient.DeleteMessageAsync(messageId, popReceipt);
        }

        public async Task<int> GetApproximateMessageCountAsync()
        {
            var properties = await _queueClient.GetPropertiesAsync();
            return properties.Value.ApproximateMessagesCount;
        }
    }
}
