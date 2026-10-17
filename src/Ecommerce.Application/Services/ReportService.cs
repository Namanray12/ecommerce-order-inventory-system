using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces.Repositories;
using Ecommerce.Application.Interfaces.Services;
using Ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<TopCustomerReportDto>> GetTopCustomersAsync(int days, int limit = 10, CancellationToken cancellationToken = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            var ordersQuery = _unitOfWork.Orders.AsQueryable()
                .Where(o => o.CreatedAt >= cutoffDate && o.Status != OrderStatus.Cancelled);

            var topCustomers = await ordersQuery
                .GroupBy(o => new { o.CustomerId, o.Customer.FullName, o.Customer.Email })
                .Select(g => new TopCustomerReportDto
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.FullName,
                    Email = g.Key.Email,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.Total)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(limit)
                .ToListAsync(cancellationToken);

            return topCustomers;
        }
    }
}