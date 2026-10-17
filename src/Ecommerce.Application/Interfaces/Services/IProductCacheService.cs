using Ecommerce.Application.DTOs;
using Ecommerce.Application.Common;

namespace Ecommerce.Application.Interfaces.Services
{
    public interface IProductCacheService
    {
        Task<PagedResult<ProductDto>> GetCachedProductsAsync(ProductQueryParameters parameters, Func<Task<PagedResult<ProductDto>>> factory, CancellationToken cancellationToken = default);
        void InvalidateProductCache();
    }
}