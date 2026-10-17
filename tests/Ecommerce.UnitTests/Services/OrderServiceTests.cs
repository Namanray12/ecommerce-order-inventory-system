using AutoMapper;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces.Repositories;
using Ecommerce.Application.Interfaces.Services;
using Ecommerce.Application.Services;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Exceptions;
using FluentAssertions;
using Moq;
using System.Timers;
using Xunit;

namespace Ecommerce.UnitTests.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IProductCacheService> _cacheMock;
        private readonly IMapper _mapper;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _uowMock = new Mock<IUnitOfWork>();
            _cacheMock = new Mock<IProductCacheService>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Order, OrderDto>()
                   .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                   .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));
                cfg.CreateMap<OrderItem, OrderItemDto>()
                   .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));
            });
            _mapper = config.CreateMapper();

            _orderService = new OrderService(_uowMock.Object, _cacheMock.Object, _mapper);
        }

        [Fact]
        public async Task CreateOrder_ShouldFail_WhenStockIsInsufficient_Rule1()
        {
            // Arrange
            int customerId = 1;
            string idempotencyKey = "key-123";
            var dto = new CreateOrderDto
            {
                OrderItems = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 10 } }
            };

            _uowMock.Setup(u => u.IdempotencyRecords.GetByKeyAndCustomerAsync(idempotencyKey, customerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((IdempotencyRecord?)null);

            var product = new Product { Id = 1, Name = "Test Product", Price = 100m, StockQuantity = 2, IsActive = true };
            _uowMock.Setup(u => u.Products.GetProductsByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Product> { product });

            // Act & Assert
            var action = async () => await _orderService.CreateOrderAsync(customerId, idempotencyKey, dto);
            await action.Should().ThrowAsync<DomainException>().WithMessage("*Insufficient stock*");
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnOriginalOrder_WhenIdempotencyKeyRepeated_Rule3()
        {
            // Arrange
            int customerId = 1;
            string idempotencyKey = "duplicate-key";
            var dto = new CreateOrderDto();

            var existingRecord = new IdempotencyRecord { Key = idempotencyKey, CustomerId = customerId, OrderId = 99 };
            _uowMock.Setup(u => u.IdempotencyRecords.GetByKeyAndCustomerAsync(idempotencyKey, customerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingRecord);

            var existingOrder = new Order { Id = 99, CustomerId = customerId, Status = OrderStatus.Pending, SubTotal = 500m, Total = 500m };
            _uowMock.Setup(u => u.Orders.GetOrderWithItemsByIdAsync(99, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(existingOrder);

            // Act
            var result = await _orderService.CreateOrderAsync(customerId, idempotencyKey, dto);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(99);
            _uowMock.Verify(u => u.Products.GetProductsByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task UpdateOrderStatus_ShouldRestoreStock_WhenCancelled_Rule4()
        {
            // Arrange
            int orderId = 10;
            int customerId = 1;
            var product = new Product { Id = 1, Name = "Laptop", StockQuantity = 5, Price = 1000m };
            var order = new Order
            {
                Id = orderId,
                CustomerId = customerId,
                Status = OrderStatus.Pending,
                OrderItems = new List<OrderItem> { new() { ProductId = 1, Quantity = 2, Product = product } }
            };

            _uowMock.Setup(u => u.Orders.GetOrderWithItemsByIdAsync(orderId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(order);

            // Act
            var result = await _orderService.UpdateOrderStatusAsync(orderId, "Cancelled", customerId, isAdmin: false);

            // Assert
            result.Status.Should().Be("Cancelled");
            product.StockQuantity.Should().Be(7); // 5 + 2 restored
            _uowMock.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task CreateOrder_ShouldApplyCorrectDiscounts_Rule5()
        {
            // Arrange
            int customerId = 1;
            string idempotencyKey = "disc-key";
            var dto = new CreateOrderDto
            {
                OrderItems = new List<CreateOrderItemDto> { new() { ProductId = 1, Quantity = 3 } } 
            };

            _uowMock.Setup(u => u.IdempotencyRecords.GetByKeyAndCustomerAsync(idempotencyKey, customerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((IdempotencyRecord?)null);

            var product = new Product { Id = 1, Name = "High Item", Price = 4000.00m, StockQuantity = 10, IsActive = true };
            _uowMock.Setup(u => u.Products.GetProductsByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<Product> { product });

            _uowMock.Setup(u => u.Orders.GetCustomerOrderCountAsync(customerId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(4);

            var createdOrderEntity = new Order
            {
                Id = 1,
                CustomerId = customerId,
                Status = OrderStatus.Pending,
                SubTotal = 12000.00m,
                Discount = 1800.00m,
                Total = 10200.00m,
                OrderItems = new List<OrderItem> { new() { ProductId = 1, Quantity = 3, UnitPrice = 4000.00m, Product = product } }
            };

            _uowMock.Setup(u => u.Orders.GetOrderWithItemsByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(createdOrderEntity);

            // Act
            var result = await _orderService.CreateOrderAsync(customerId, idempotencyKey, dto);

            // Assert
            result.SubTotal.Should().Be(12000.00m);
            result.Discount.Should().Be(1800.00m);
            result.Total.Should().Be(10200.00m);
        }
    }
}