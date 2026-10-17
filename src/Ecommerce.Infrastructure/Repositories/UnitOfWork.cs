using Ecommerce.Application.Interfaces.Repositories;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ecommerce.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _currentTransaction;

        public IUserRepository Users { get; }
        public IRefreshTokenRepository RefreshTokens { get; }
        public IProductRepository Products { get; }
        public IOrderRepository Orders { get; }
        public IIdempotencyRepository IdempotencyRecords { get; }

        public UnitOfWork(
            ApplicationDbContext context,
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IIdempotencyRepository idempotencyRepository)
        {
            _context = context;
            Users = userRepository;
            RefreshTokens = refreshTokenRepository;
            Products = productRepository;
            Orders = orderRepository;
            IdempotencyRecords = idempotencyRepository;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null) return;
            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                if (_currentTransaction != null)
                {
                    await _currentTransaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.RollbackAsync(cancellationToken);
                }
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
        }
    }
}