using Ecommerce.Application.Interfaces.Repositories;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories
{
    public class IdempotencyRepository : GenericRepository<IdempotencyRecord>, IIdempotencyRepository
    {
        public IdempotencyRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IdempotencyRecord?> GetByKeyAndCustomerAsync(string key, int customerId, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(i => i.Key == key && i.CustomerId == customerId, cancellationToken);
    }
}