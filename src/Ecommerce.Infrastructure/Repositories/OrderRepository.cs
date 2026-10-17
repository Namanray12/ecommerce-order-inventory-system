using Ecommerce.Application.Interfaces.Repositories;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Order?> GetOrderWithItemsByIdAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        }

        public async Task<int> GetCustomerOrderCountAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return await _dbSet.CountAsync(o => o.CustomerId == customerId && o.Status != OrderStatus.Cancelled, cancellationToken);
        }

        public async Task<List<Order>> GetExpiredPendingOrdersAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt <= cutoffUtc)
                .ToListAsync(cancellationToken);
        }
    }
}