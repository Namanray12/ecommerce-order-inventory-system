using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces.Repositories
{
    public interface IIdempotencyRepository : IGenericRepository<IdempotencyRecord>
    {
        Task<IdempotencyRecord?> GetByKeyAndCustomerAsync(string key, int customerId, CancellationToken cancellationToken = default);
    }
}