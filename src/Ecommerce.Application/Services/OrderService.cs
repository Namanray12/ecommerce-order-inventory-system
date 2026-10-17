using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces.Repositories;
using Ecommerce.Application.Interfaces.Services;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductCacheService _productCacheService;
        private readonly IMapper _mapper;

        public OrderService(IUnitOfWork unitOfWork, IProductCacheService productCacheService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _productCacheService = productCacheService;
            _mapper = mapper;
        }

        public async Task<OrderDto> CreateOrderAsync(int customerId, string idempotencyKey, CreateOrderDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new DomainException("Idempotency-Key header is required.");

            var existingRecord = await _unitOfWork.IdempotencyRecords.GetByKeyAndCustomerAsync(idempotencyKey, customerId, cancellationToken);
            if (existingRecord != null)
            {
                var existingOrder = await _unitOfWork.Orders.GetOrderWithItemsByIdAsync(existingRecord.OrderId, cancellationToken);
                if (existingOrder != null)
                    return _mapper.Map<OrderDto>(existingOrder);
            }

            if (dto.OrderItems == null || !dto.OrderItems.Any())
                throw new DomainException("Order must contain at least one item.");

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var productIds = dto.OrderItems.Select(i => i.ProductId).Distinct().ToList();
                var products = await _unitOfWork.Products.GetProductsByIdsAsync(productIds, cancellationToken);

                var orderItems = new List<OrderItem>();
                decimal subTotal = 0m;

                foreach (var itemDto in dto.OrderItems)
                {
                    var product = products.FirstOrDefault(p => p.Id == itemDto.ProductId);
                    if (product == null || !product.IsActive)
                        throw new NotFoundException($"Product with ID {itemDto.ProductId} was not found or is inactive.");

                    if (product.StockQuantity < itemDto.Quantity)
                        throw new DomainException($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}, Requested: {itemDto.Quantity}");

                    product.StockQuantity -= itemDto.Quantity;
                    _unitOfWork.Products.Update(product);

                    var unitPrice = product.Price;
                    subTotal += unitPrice * itemDto.Quantity;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = itemDto.Quantity,
                        UnitPrice = unitPrice
                    });
                }

                decimal discount = 0m;
                if (subTotal > 10000m)
                {
                    discount += subTotal * 0.05m;
                }

                int priorOrderCount = await _unitOfWork.Orders.GetCustomerOrderCountAsync(customerId, cancellationToken);
                bool isEveryFifthOrder = (priorOrderCount + 1) % 5 == 0;
                if (isEveryFifthOrder)
                {
                    discount += subTotal * 0.10m;
                }

                subTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);
                discount = Math.Round(discount, 2, MidpointRounding.AwayFromZero);
                decimal total = Math.Round(subTotal - discount, 2, MidpointRounding.AwayFromZero);

                var order = new Order
                {
                    CustomerId = customerId,
                    Status = OrderStatus.Pending,
                    SubTotal = subTotal,
                    Discount = discount,
                    Total = total,
                    CreatedAt = DateTime.UtcNow,
                    OrderItems = orderItems
                };

                await _unitOfWork.Orders.AddAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Save Idempotency Record
                var idempotencyRecord = new IdempotencyRecord
                {
                    Key = idempotencyKey,
                    CustomerId = customerId,
                    OrderId = order.Id
                };
                await _unitOfWork.IdempotencyRecords.AddAsync(idempotencyRecord, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _productCacheService.InvalidateProductCache();

                var createdOrder = await _unitOfWork.Orders.GetOrderWithItemsByIdAsync(order.Id, cancellationToken);
                return _mapper.Map<OrderDto>(createdOrder);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw new ConcurrencyConflictException("Concurrency conflict occurred: stock was modified by another transaction. Please try again.", ex);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<OrderDto> GetOrderByIdAsync(int orderId, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var order = await _unitOfWork.Orders.GetOrderWithItemsByIdAsync(orderId, cancellationToken);
            if (order == null)
                throw new NotFoundException($"Order with ID {orderId} was not found.");

            if (!isAdmin && order.CustomerId != currentUserId)
                throw new ForbiddenAccessException("You do not have permission to view this order.");

            return _mapper.Map<OrderDto>(order);
        }

        public async Task<PagedResult<OrderDto>> GetCustomerOrdersAsync(int customerId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Orders.AsQueryable().Where(o => o.CustomerId == customerId);
            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<OrderDto>
            {
                Items = _mapper.Map<List<OrderDto>>(items),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<OrderDto> UpdateOrderStatusAsync(int orderId, string newStatusStr, int currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var order = await _unitOfWork.Orders.GetOrderWithItemsByIdAsync(orderId, cancellationToken);
            if (order == null)
                throw new NotFoundException($"Order with ID {orderId} was not found.");

            if (!Enum.TryParse<OrderStatus>(newStatusStr, true, out var newStatus))
                throw new DomainException("Invalid order status provided.");

            if (!isAdmin)
            {
                if (order.CustomerId != currentUserId)
                    throw new ForbiddenAccessException("You can only modify your own orders.");

                if (newStatus != OrderStatus.Cancelled || order.Status != OrderStatus.Pending)
                    throw new DomainException("Customers may only cancel their own Pending orders.");
            }

            var currentStatus = order.Status;

            
            bool isValidTransition = (currentStatus == OrderStatus.Pending && (newStatus == OrderStatus.Confirmed || newStatus == OrderStatus.Cancelled)) ||
                                     (currentStatus == OrderStatus.Confirmed && (newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Cancelled));

            if (!isValidTransition)
                throw new DomainException($"Invalid status transition from {currentStatus} to {newStatus}.");

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                if (newStatus == OrderStatus.Cancelled && currentStatus != OrderStatus.Cancelled)
                {
                    foreach (var item in order.OrderItems)
                    {
                        item.Product.StockQuantity += item.Quantity;
                        _unitOfWork.Products.Update(item.Product);
                    }
                    _productCacheService.InvalidateProductCache();
                }

                order.Status = newStatus;
                _unitOfWork.Orders.Update(order);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return _mapper.Map<OrderDto>(order);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}