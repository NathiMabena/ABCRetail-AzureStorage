using ABCRetail.AzureStorage.Models;
using ABCRetail.AzureStorage.Services;

namespace ABCRetail.AzureStorage.Workers
{
    public class OrderProcessingWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderProcessingWorker> _logger;

        public OrderProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<OrderProcessingWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderProcessingWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Services are scoped, so we create a fresh scope each loop
                    using var scope = _scopeFactory.CreateScope();
                    var queueService = scope.ServiceProvider.GetRequiredService<QueueStorageService>();
                    var fileService = scope.ServiceProvider.GetRequiredService<FileStorageService>();
                    var tableService = scope.ServiceProvider.GetRequiredService<TableStorageService>();

                    var message = await queueService.ReceiveMessageAsync();

                    if (message != null)
                    {
                        // Simulate processing work
                        await Task.Delay(500, stoppingToken);

                        // The message text is the Order's RowKey
                        var orderId = message.MessageText;
                        var order = await tableService.GetEntityAsync<OrderEntity>("Order", orderId);

                        string logEntry;

                        if (order == null)
                        {
                            logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Could not find Order {orderId} — skipping.";
                        }
                        else
                        {
                            var product = await tableService.GetEntityAsync<ProductEntity>("Product", order.ProductId);

                            if (product != null)
                            {
                                // Decrement stock, never below zero
                                product.StockQuantity = Math.Max(0, product.StockQuantity - order.Quantity);
                                await tableService.UpdateEntityAsync(product);
                            }

                            order.Status = "Processed";
                            await tableService.UpdateEntityAsync(order);

                            logEntry =
                                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Processed Order {order.RowKey}: " +
                                $"{order.Quantity} x \"{product?.Name ?? "(unknown product)"}\" — " +
                                $"remaining stock: {product?.StockQuantity.ToString() ?? "n/a"} (MessageId: {message.MessageId})";
                        }

                        await fileService.WriteLogAsync(logEntry);
                        await queueService.DeleteMessageAsync(message.MessageId, message.PopReceipt);

                        _logger.LogInformation("Processed and logged order {OrderId}", orderId);
                    }
                    else
                    {
                        // No messages — wait a bit before polling again
                        await Task.Delay(3000, stoppingToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Expected during shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OrderProcessingWorker loop.");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}