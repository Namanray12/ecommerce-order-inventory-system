using Ecommerce.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Ecommerce.IntegrationTests.Concurrency
{
    public class OrderConcurrencyTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public OrderConcurrencyTests(WebApplicationFactory<Program> initializer)
        {
            _factory = initializer;
        }

        [Fact]
        public async Task ParallelOrdersForLastUnit_ShouldResultInOneSuccessAndOneConflict_Rule2()
        {
            // Arrange
            var client = _factory.CreateClient();

            var orderDto = new CreateOrderDto
            {
                OrderItems = new List<CreateOrderItemDto> { new() { ProductId = 15, Quantity = 1 } }
            };

            // Simulating parallel tasks
            var task1 = client.PostAsJsonAsync("/api/orders", orderDto);
            var task2 = client.PostAsJsonAsync("/api/orders", orderDto);

            var responses = await Task.WhenAll(task1, task2);

            responses.Select(r => r.StatusCode).Should().Contain(HttpStatusCode.Conflict);
        }
    }
}