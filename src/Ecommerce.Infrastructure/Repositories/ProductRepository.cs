using Ecommerce.Application.Interfaces.Repositories;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context) { }

        public async Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null, CancellationToken cancellationToken = default)
        {
            var normalizedSku = sku.Trim().ToUpper();
            return await _dbSet.AnyAsync(p => p.Sku.ToUpper() == normalizedSku && (!excludeProductId.HasValue || p.Id != excludeProductId.Value), cancellationToken);
        }

        public async Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.Distinct().ToList();
            return await _dbSet.Where(p => idList.Contains(p.Id)).ToListAsync(cancellationToken);
        }
    }
}