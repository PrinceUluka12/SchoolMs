using SchoolMS.Core.DTOs.Auth;

namespace SchoolMS.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<MfaSetupDto> SetupMfaAsync(Guid userId);
    Task<AuthResponseDto> VerifyMfaAsync(MfaVerifyDto request, string ipAddress);
    Task LogoutAsync(Guid userId);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordDto request);
}