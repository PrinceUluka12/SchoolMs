using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using SchoolMS.Core.DTOs.Auth;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SchoolMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string ipAddress)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated. Contact the administrator.");

        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            throw new UnauthorizedAccessException($"Account locked. Try again after {user.LockedUntil:HH:mm} UTC.");

        // Reset failed attempts on successful credential check
        user.FailedLoginAttempts = 0;
        user.LastLoginAt = DateTime.UtcNow;

        if (user.MfaEnabled)
        {
            // Issue short-lived MFA token (10 minutes)
            var mfaToken = GenerateMfaToken(user.Id);
            await _db.SaveChangesAsync();
            return new AuthResponseDto { MfaRequired = true, MfaToken = mfaToken, Role = user.Role, UserId = user.Id, Email = user.Email };
        }

        return await IssueTokens(user, ipAddress);
    }

    public async Task<AuthResponseDto> VerifyMfaAsync(MfaVerifyDto request, string ipAddress)
    {
        var userId = ValidateMfaToken(request.MfaToken);
        var user = await _db.Users.FindAsync(userId)
            ?? throw new UnauthorizedAccessException("Invalid MFA session.");

        if (string.IsNullOrEmpty(user.MfaSecret))
            throw new InvalidOperationException("MFA not configured for this account.");

        var totp = new Totp(Base32Encoding.ToBytes(user.MfaSecret));
        if (!totp.VerifyTotp(request.Code, out _, new VerificationWindow(1, 1)))
            throw new UnauthorizedAccessException("Invalid or expired MFA code.");

        return await IssueTokens(user, ipAddress);
    }

    public async Task<MfaSetupDto> SetupMfaAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var key = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(key);
        user.MfaSecret = secret;
        user.MfaEnabled = true;
        await _db.SaveChangesAsync();

        var qrUri = $"otpauth://totp/SchoolMS:{Uri.EscapeDataString(user.Email)}?secret={secret}&issuer=SchoolMS";
        return new MfaSetupDto { Secret = secret, QrCodeUri = qrUri };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired. Please log in again.");

        return await IssueTokens(user, ipAddress);
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _db.SaveChangesAsync();
        }
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
        if (user == null) return; // Don't reveal whether email exists

        // TODO Sprint 3: integrate email service to send reset link
        // For now: log the token (dev only)
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Console.WriteLine($"[DEV] Password reset token for {email}: {token}");
    }

    public Task ResetPasswordAsync(ResetPasswordDto request)
    {
        // Full implementation in Sprint 3 with email service
        throw new NotImplementedException("Email service not yet configured.");
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<AuthResponseDto> IssueTokens(User user, string ipAddress)
    {
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");
        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
        var accessToken = GenerateJwt(user, expiry);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            MfaRequired = false,
            Role = user.Role,
            UserId = user.Id,
            Email = user.Email,
            ExpiresAt = expiry
        };
    }

    private string GenerateJwt(User user, DateTime expiry)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateMfaToken(Guid userId)
    {
        // Short-lived JWT scoped only for MFA completion
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: new[] { new Claim("mfa_uid", userId.ToString()), new Claim("scope", "mfa") },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private Guid ValidateMfaToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidIssuer = _config["Jwt:Issuer"],
            ValidAudience = _config["Jwt:Audience"],
            ValidateLifetime = true
        }, out _);
        var uid = principal.FindFirst("mfa_uid")?.Value
             ?? throw new UnauthorizedAccessException("Invalid MFA token.");
        return Guid.Parse(uid);
    }
}