using Ecommerce.Application.Common;
using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(int customerId, string idempotencyKey, CreateOrderDto dto, CancellationToken cancellationToken = default);
        Task<OrderDto> GetOrderByIdAsync(int orderId, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
        Task<PagedResult<OrderDto>> GetCustomerOrdersAsync(int customerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
        Task<OrderDto> UpdateOrderStatusAsync(int orderId, string newStatus, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    }
}