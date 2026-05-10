namespace SchoolMS.Core.Entities;


public class User : BaseEntity
{
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Role { get; set; } = null!;   // Admin, Teacher, Student, Parent, Finance, Librarian, Transport, Warden
    public bool IsActive { get; set; } = true;
    public bool MfaEnabled { get; set; } = false;
    public string? MfaSecret { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}