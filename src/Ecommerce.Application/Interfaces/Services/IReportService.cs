using Ecommerce.Application.DTOs;

namespace Ecommerce.Application.Interfaces.Services
{
    public interface IReportService
    {
        Task<List<TopCustomerReportDto>> GetTopCustomersAsync(int days, int limit = 10, CancellationToken cancellationToken = default);
    }
}