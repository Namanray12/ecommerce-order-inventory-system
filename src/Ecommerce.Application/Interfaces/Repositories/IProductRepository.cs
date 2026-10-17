using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null, CancellationToken cancellationToken = default);
        Task<List<Product>> GetProductsByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
    }
}