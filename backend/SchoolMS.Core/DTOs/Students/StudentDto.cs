using Microsoft.AspNetCore.Http;

namespace SchoolMS.Core.DTOs.Students;

// ── Create ────────────────────────────────────────────────────────────────────
public class CreateStudentDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = null!;
    public Guid? ClassId { get; set; }
    public string? Address { get; set; }
    public string? MedicalNotes { get; set; }

    // Guardian — creates guardian + portal account if new
    public Guid? ExistingGuardianId { get; set; }  // link to existing guardian
    public CreateGuardianDto? NewGuardian { get; set; } // or create new one
}

public class CreateGuardianDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? AlternatePhone { get; set; }
    public string Relationship { get; set; } = null!;
    public string? Address { get; set; }
    public string? Occupation { get; set; }
}

// ── Update ────────────────────────────────────────────────────────────────────
public class UpdateStudentDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? MedicalNotes { get; set; }
}

public class UpdateGuardianDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Relationship { get; set; }
    public string? Address { get; set; }
    public string? Occupation { get; set; }
}

// ── Transfer ──────────────────────────────────────────────────────────────────
public class TransferStudentDto
{
    public Guid NewClassId { get; set; }
    public string? Reason { get; set; }
}

// ── Document Upload ───────────────────────────────────────────────────────────
public class UploadDocumentDto
{
    public string Name { get; set; } = null!; // e.g. "Birth Certificate"
    public IFormFile File { get; set; } = null!;
}

// ── Response ──────────────────────────────────────────────────────────────────
public class StudentResponseDto
{
    public Guid Id { get; set; }
    public string StudentNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public DateTime DateOfBirth { get; set; }
    public int Age => (int)((DateTime.UtcNow - DateOfBirth).TotalDays / 365.25);
    public string Gender { get; set; } = null!;
    public string? PhotoUrl { get; set; }
    public string Status { get; set; } = null!;
    public string? Address { get; set; }
    public string? MedicalNotes { get; set; }
    public Guid? ClassId { get; set; }
    public string? ClassName { get; set; }
    public string? AcademicYear { get; set; }
    public GuardianSummaryDto? Guardian { get; set; }
    public List<StudentDocumentResponseDto> Documents { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class GuardianResponseDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? AlternatePhone { get; set; }
    public string Relationship { get; set; } = null!;
    public string? Address { get; set; }
    public string? Occupation { get; set; }
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GuardianSummaryDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Relationship { get; set; } = null!;
}

public class StudentDocumentResponseDto
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

// ── Bulk Import ───────────────────────────────────────────────────────────────
public class BulkImportResultDto
{
    public int TotalRows { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<BulkImportErrorDto> Errors { get; set; } = new();
}

public class BulkImportErrorDto
{
    public int Row { get; set; }
    public string Field { get; set; } = null!;
    public string Message { get; set; } = null!;
}

// ── CSV Row Model ─────────────────────────────────────────────────────────────
public class StudentCsvRow
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string DateOfBirth { get; set; } = null!; // dd/MM/yyyy
    public string Gender { get; set; } = null!;
    public string? ClassName { get; set; }
    public string GuardianFirstName { get; set; } = null!;
    public string GuardianLastName { get; set; } = null!;
    public string GuardianEmail { get; set; } = null!;
    public string GuardianPhone { get; set; } = null!;
    public string GuardianRelationship { get; set; } = null!;
}