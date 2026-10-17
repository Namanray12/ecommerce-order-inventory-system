using AutoMapper;
using Ecommerce.Application.Common;
using Ecommerce.Application.DTOs;
using Ecommerce.Application.Interfaces.Repositories;
using Ecommerce.Application.Interfaces.Services;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductCacheService _cacheService;
        private readonly IMapper _mapper;

        public ProductService(IUnitOfWork unitOfWork, IProductCacheService cacheService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _mapper = mapper;
        }

        public async Task<PagedResult<ProductDto>> GetProductsAsync(ProductQueryParameters parameters, CancellationToken cancellationToken = default)
        {
            return await _cacheService.GetCachedProductsAsync(parameters, async () =>
            {
                var query = _unitOfWork.Products.AsQueryable().Where(p => p.IsActive);

                if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
                {
                    var term = parameters.SearchTerm.Trim().ToLower();
                    query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
                }

                if (parameters.MinPrice.HasValue)
                {
                    query = query.Where(p => p.Price >= parameters.MinPrice.Value);
                }

                if (parameters.MaxPrice.HasValue)
                {
                    query = query.Where(p => p.Price <= parameters.MaxPrice.Value);
                }

                if (parameters.InStock.HasValue && parameters.InStock.Value)
                {
                    query = query.Where(p => p.StockQuantity > 0);
                }

                // Sorting
                query = parameters.SortBy?.ToLower() switch
                {
                    "price" => parameters.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                    "stock" => parameters.SortDescending ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
                    _ => parameters.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
                };

                var totalCount = await query.CountAsync(cancellationToken);

                var items = await query
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToListAsync(cancellationToken);

                return new PagedResult<ProductDto>
                {
                    Items = _mapper.Map<List<ProductDto>>(items),
                    PageNumber = parameters.PageNumber,
                    PageSize = parameters.PageSize,
                    TotalCount = totalCount
                };
            }, cancellationToken);
        }

        public async Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
            if (product == null || !product.IsActive)
                throw new NotFoundException($"Product with ID {id} was not found.");

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
        {
            if (await _unitOfWork.Products.SkuExistsAsync(dto.Sku, null, cancellationToken))
                throw new DomainException($"Product with SKU '{dto.Sku}' already exists.");

            var product = _mapper.Map<Product>(dto);
            await _unitOfWork.Products.AddAsync(product, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _cacheService.InvalidateProductCache();

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id, cancellationToken);
            if (product == null)
                throw new NotFoundException($"Product with ID {id} was not found.");

            _mapper.Map(dto, product);
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _cacheService.InvalidateProductCache();

            return _mapper.Map<ProductDto>(product);
        }
    }
}