using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces.Repositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetOrderWithItemsByIdAsync(int orderId, CancellationToken cancellationToken = default);
        Task<int> GetCustomerOrderCountAsync(int customerId, CancellationToken cancellationToken = default);
        Task<List<Order>> GetExpiredPendingOrdersAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);
    }
}