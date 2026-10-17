using System.Threading.RateLimiting;

namespace Ecommerce.API.Extensions
{
    public static class RateLimiterExtensions
    {
        public static IServiceCollection AddLoginRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("LoginRateLimit", context =>
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                });

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(new { error = "Too many login attempts. Please try again later." }, token);
                };
            });

            return services;
        }
    }
}