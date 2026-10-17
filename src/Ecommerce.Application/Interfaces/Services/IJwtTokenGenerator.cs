using Ecommerce.Domain.Entities;

namespace Ecommerce.Application.Interfaces.Services
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
    }
}