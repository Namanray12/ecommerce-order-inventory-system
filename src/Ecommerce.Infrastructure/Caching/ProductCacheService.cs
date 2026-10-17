using Ecommerce.Application.Common;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Ecommerce.Infrastructure.Caching
{
    public class ProductCacheService : IProductCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private static CancellationTokenSource _resetCacheToken = new();

        public ProductCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task<PagedResult<ProductDto>> GetCachedProductsAsync(
            ProductQueryParameters parameters,
            Func<Task<PagedResult<ProductDto>>> factory,
            CancellationToken cancellationToken = default)
        {
            string cacheKey = $"products_{parameters.PageNumber}_{parameters.PageSize}_{parameters.SearchTerm}_{parameters.MinPrice}_{parameters.MaxPrice}_{parameters.InStock}_{parameters.SortBy}_{parameters.SortDescending}";

            if (!_memoryCache.TryGetValue(cacheKey, out PagedResult<ProductDto>? result))
            {
                result = await factory();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));

                _memoryCache.Set(cacheKey, result, cacheEntryOptions);
            }

            return result!;
        }

        public void InvalidateProductCache()
        {
            Interlocked.Exchange(ref _resetCacheToken, new CancellationTokenSource()).Cancel();
        }
    }
}