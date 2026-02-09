namespace ShopHangTet.Services
{
    /// Interface cho OTP Service - quản lý mã xác thực
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string email);
        Task<bool> ValidateOtpAsync(string email, string otp);
        Task<bool> InvalidateOtpAsync(string email);
    }
}
