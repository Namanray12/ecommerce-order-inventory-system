using System.Security.Claims;
using Ecommerce.Application.Common;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
        {
            var customerId = GetCurrentUserId();
            var idempotencyKey = Request.Headers["Idempotency-Key"].ToString();

            var order = await _orderService.CreateOrderAsync(customerId, idempotencyKey, dto, cancellationToken);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult<PagedResult<OrderDto>>> GetMyOrders([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var customerId = GetCurrentUserId();
            var result = await _orderService.GetCustomerOrdersAsync(customerId, pageNumber, pageSize, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(int id, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var order = await _orderService.GetOrderByIdAsync(id, userId, isAdmin, cancellationToken);
            return Ok(order);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<OrderDto>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto, CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            var order = await _orderService.UpdateOrderStatusAsync(id, dto.Status, userId, isAdmin, cancellationToken);
            return Ok(order);
        }

        private int GetCurrentUserId()
        {
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.Parse(subClaim!);
        }
    }
}