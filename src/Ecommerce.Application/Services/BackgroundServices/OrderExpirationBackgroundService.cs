using Ecommerce.Application.Interfaces.Repositories;
using Ecommerce.Application.Interfaces.Services;
using Ecommerce.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.BackgroundServices
{
    public class OrderExpirationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderExpirationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public OrderExpirationBackgroundService(IServiceProvider serviceProvider, ILogger<OrderExpirationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Order Expiration Background Service is starting.");

            using var timer = new PeriodicTimer(_checkInterval);

            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await CancelExpiredPendingOrdersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing expired pending orders.");
                }
            }
        }

        private async Task CancelExpiredPendingOrdersAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var cacheService = scope.ServiceProvider.GetRequiredService<IProductCacheService>();

            var cutoffUtc = DateTime.UtcNow.AddMinutes(-30);
            var expiredOrders = await unitOfWork.Orders.GetExpiredPendingOrdersAsync(cutoffUtc, cancellationToken);

            if (!expiredOrders.Any()) return;

            foreach (var order in expiredOrders)
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    order.Status = OrderStatus.Cancelled;
                    unitOfWork.Orders.Update(order);

                    foreach (var item in order.OrderItems)
                    {
                        item.Product.StockQuantity += item.Quantity;
                        unitOfWork.Products.Update(item.Product);
                    }

                    await unitOfWork.CommitTransactionAsync(cancellationToken);
                    cacheService.InvalidateProductCache();

                    _logger.LogInformation("Automatically cancelled expired Pending order ID {OrderId} and restored stock.", order.Id);
                }
                catch (Exception ex)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _logger.LogError(ex, "Failed to cancel expired Pending order ID {OrderId}.", order.Id);
                }
            }
        }
    }
}