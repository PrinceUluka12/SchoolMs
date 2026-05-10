using Microsoft.AspNetCore.Http;

namespace SchoolMS.Core.DTOs.Staff;

// ── Create ────────────────────────────────────────────────────────────────────
public class CreateStaffDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string Role { get; set; } = null!;
    public string ContractType { get; set; } = null!;
    public DateTime JoinDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Qualifications { get; set; }
    public string? Address { get; set; }
}

// ── Update ────────────────────────────────────────────────────────────────────
public class UpdateStaffDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? Role { get; set; }
    public string? ContractType { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Qualifications { get; set; }
    public string? Address { get; set; }
}

// ── Filter ────────────────────────────────────────────────────────────────────
public class StaffFilterDto
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Status { get; set; }
    public string? ContractType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "LastName";
    public string SortDirection { get; set; } = "asc";
}

// ── Document Upload ───────────────────────────────────────────────────────────
public class UploadStaffDocumentDto
{
    public string Name { get; set; } = null!;
    public IFormFile File { get; set; } = null!;
}

// ── Response ──────────────────────────────────────────────────────────────────
public class StaffResponseDto
{
    public Guid Id { get; set; }
    public string StaffNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string ContractType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? PhotoUrl { get; set; }
    public string? Address { get; set; }
    public string? Qualifications { get; set; }
    public DateTime JoinDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public List<StaffDocumentResponseDto> Documents { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class StaffDocumentResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public string FileSizeFormatted => FileSizeBytes < 1024 * 1024
        ? $"{FileSizeBytes / 1024.0:F1} KB"
        : $"{FileSizeBytes / (1024.0 * 1024):F1} MB";
    public DateTime CreatedAt { get; set; }
}