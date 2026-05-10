namespace SchoolMS.Core.DTOs.Auth;

public class LoginRequestDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class AuthResponseDto
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public bool MfaRequired { get; set; } = false;
    public string? MfaToken { get; set; } // short-lived token used to complete MFA step
    public string Role { get; set; } = null!;
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

public class MfaSetupDto
{
    public string Secret { get; set; } = null!;
    public string QrCodeUri { get; set; } = null!; // otpauth:// URI for authenticator app
}

public class MfaVerifyDto
{
    public string MfaToken { get; set; } = null!; // temp token from login step
    public string Code { get; set; } = null!;     // 6-digit TOTP code
}

public class ResetPasswordDto
{
    public string Token { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}