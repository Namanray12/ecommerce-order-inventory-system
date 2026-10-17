using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Data.Seed
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                if (context.Database.IsRelational())
                {
                    await context.Database.MigrateAsync();
                }

                if (!await context.Users.AnyAsync())
                {
                    var users = new List<User>
                    {
                        new User
                        {
                            FullName = "System Admin",
                            Email = "admin@ecommerce.com",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                            Role = UserRole.Admin
                        },
                        new User
                        {
                            FullName = "Alice Sharma",
                            Email = "alice@customer.com",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                            Role = UserRole.Customer
                        },
                        new User
                        {
                            FullName = "Rahul Verma",
                            Email = "rahul@customer.com",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                            Role = UserRole.Customer
                        },
                        new User
                        {
                            FullName = "Priya Patel",
                            Email = "priya@customer.com",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                            Role = UserRole.Customer
                        }
                    };

                    await context.Users.AddRangeAsync(users);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded 1 Admin and 3 Customer users successfully.");
                }

                if (!await context.Products.AnyAsync())
                {
                    var products = new List<Product>
                    {
                        new() { Sku = "SKU-1001", Name = "Pro Wireless Mechanical Keyboard", Price = 4500.00m, StockQuantity = 25, IsActive = true },
                        new() { Sku = "SKU-1002", Name = "UltraWide 34-inch Curved Monitor", Price = 32500.00m, StockQuantity = 10, IsActive = true },
                        new() { Sku = "SKU-1003", Name = "Ergonomic Mesh Office Chair", Price = 14999.00m, StockQuantity = 15, IsActive = true },
                        new() { Sku = "SKU-1004", Name = "Noise Cancelling Headphones X5", Price = 8999.00m, StockQuantity = 30, IsActive = true },
                        new() { Sku = "SKU-1005", Name = "USB-C 10-in-1 Docking Station", Price = 3499.00m, StockQuantity = 50, IsActive = true },
                        new() { Sku = "SKU-1006", Name = "4K Auto-Focus Streaming Webcam", Price = 5999.00m, StockQuantity = 20, IsActive = true },
                        new() { Sku = "SKU-1007", Name = "1TB NVMe Gen4 Internal SSD", Price = 7499.00m, StockQuantity = 40, IsActive = true },
                        new() { Sku = "SKU-1008", Name = "2TB Rugged External Portable SSD", Price = 12499.00m, StockQuantity = 18, IsActive = true },
                        new() { Sku = "SKU-1009", Name = "Precision Wireless Gaming Mouse", Price = 2999.00m, StockQuantity = 35, IsActive = true },
                        new() { Sku = "SKU-1010", Name = "Dual Monitor Heavy-Duty Desk Arm", Price = 3999.00m, StockQuantity = 22, IsActive = true },
                        new() { Sku = "SKU-1011", Name = "Smart LED Monitor Light Bar", Price = 2499.00m, StockQuantity = 45, IsActive = true },
                        new() { Sku = "SKU-1012", Name = "Wi-Fi 6E Tri-Band Mesh Router", Price = 11500.00m, StockQuantity = 12, IsActive = true },
                        new() { Sku = "SKU-1013", Name = "100W GaN Fast Desktop Charger", Price = 3299.00m, StockQuantity = 60, IsActive = true },
                        new() { Sku = "SKU-1014", Name = "Studio Condenser Podcast Microphone", Price = 6800.00m, StockQuantity = 16, IsActive = true },
                        new() { Sku = "SKU-1015", Name = "Limited Edition Collector GPU (Last Unit)", Price = 65000.00m, StockQuantity = 1, IsActive = true }
                    };

                    await context.Products.AddRangeAsync(products);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded 15 Products successfully.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                throw;
            }
        }
    }
}