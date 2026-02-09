using ShopHangTet.Models;

namespace ShopHangTet.Services
{
    /// Interface cho JWT Service - quản lý token authentication
    public interface IJwtService
    {
        string GenerateToken(UserModel user);
        string? ValidateToken(string token);
        DateTime GetTokenExpiration(string token);
    }
}
