using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Entities
{
    public class IdempotencyRecord : BaseEntity
    {
        public string Key { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}