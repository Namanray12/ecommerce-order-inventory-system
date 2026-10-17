using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configurations
{
    public class IdempotencyConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
    {
        public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
        {
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Key)
                .IsRequired()
                .HasMaxLength(128);

            builder.HasIndex(i => new { i.Key, i.CustomerId })
                .IsUnique();
        }
    }
}