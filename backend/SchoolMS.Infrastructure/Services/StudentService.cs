using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Common;
using SchoolMS.Core.DTOs.Students;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;
using System.Globalization;

namespace SchoolMS.Infrastructure.Services;

public class StudentService : IStudentService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly StudentNumberGenerator _numberGen;

    public StudentService(AppDbContext db, IFileStorageService fileStorage, StudentNumberGenerator numberGen)
    {
        _db = db;
        _fileStorage = fileStorage;
        _numberGen = numberGen;
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<StudentResponseDto> CreateAsync(CreateStudentDto dto)
    {
        if (dto.ExistingGuardianId == null && dto.NewGuardian == null)
            throw new ArgumentException("Either ExistingGuardianId or NewGuardian must be provided.");

        Guardian guardian;

        if (dto.ExistingGuardianId.HasValue)
        {
            guardian = await _db.Guardians.FindAsync(dto.ExistingGuardianId.Value)
                ?? throw new KeyNotFoundException($"Guardian {dto.ExistingGuardianId} not found.");
        }
        else
        {
            guardian = await CreateGuardianInternalAsync(dto.NewGuardian!);
        }

        // Create student portal user
        var tempPassword = GenerateTempPassword();
        var studentUser = new User
        {
            Email = GenerateStudentEmail(dto.FirstName, dto.LastName),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            Role = "Student",
            IsActive = true
        };
        _db.Users.Add(studentUser);
        await _db.SaveChangesAsync();

        var studentNumber = await _numberGen.GenerateAsync();

        var student = new Student
        {
            StudentNumber = studentNumber,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            ClassId = dto.ClassId,
            Address = dto.Address,
            MedicalNotes = dto.MedicalNotes,
            UserId = studentUser.Id,
            GuardianId = guardian.Id,
            Status = "Active"
        };

        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        return await MapToResponseAsync(student.Id);
    }

    // ── Get By Id ─────────────────────────────────────────────────────────────
    public async Task<StudentResponseDto> GetByIdAsync(Guid id)
    {
        var exists = await _db.Students.AnyAsync(s => s.Id == id);
        if (!exists) throw new KeyNotFoundException($"Student {id} not found.");
        return await MapToResponseAsync(id);
    }

    // ── Get All (paged + filtered) ────────────────────────────────────────────
    public async Task<PagedResult<StudentResponseDto>> GetAllAsync(StudentFilterDto filter)
    {
        var query = _db.Students
            .Include(s => s.Class).ThenInclude(c => c!.AcademicYear)
            .Include(s => s.Guardian)
            .Include(s => s.Documents)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(search) ||
                s.LastName.ToLower().Contains(search) ||
                s.StudentNumber.ToLower().Contains(search));
        }

        if (filter.ClassId.HasValue)
            query = query.Where(s => s.ClassId == filter.ClassId);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(s => s.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.Gender))
            query = query.Where(s => s.Gender == filter.Gender);

        if (filter.AcademicYearId.HasValue)
            query = query.Where(s => s.Class != null && s.Class.AcademicYearId == filter.AcademicYearId);

        // Sorting
        query = filter.SortBy.ToLower() switch
        {
            "firstname" => filter.SortDirection == "desc"
                ? query.OrderByDescending(s => s.FirstName)
                : query.OrderBy(s => s.FirstName),
            "studentnumber" => filter.SortDirection == "desc"
                ? query.OrderByDescending(s => s.StudentNumber)
                : query.OrderBy(s => s.StudentNumber),
            "createdat" => filter.SortDirection == "desc"
                ? query.OrderByDescending(s => s.CreatedAt)
                : query.OrderBy(s => s.CreatedAt),
            _ => filter.SortDirection == "desc"
                ? query.OrderByDescending(s => s.LastName)
                : query.OrderBy(s => s.LastName)
        };

        var total = await query.CountAsync();

        var students = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = students.Select(MapToDto).ToList();

        return new PagedResult<StudentResponseDto>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public async Task<StudentResponseDto> UpdateAsync(Guid id, UpdateStudentDto dto)
    {
        var student = await _db.Students.FindAsync(id)
            ?? throw new KeyNotFoundException($"Student {id} not found.");

        if (dto.FirstName != null) student.FirstName = dto.FirstName.Trim();
        if (dto.LastName != null) student.LastName = dto.LastName.Trim();
        if (dto.DateOfBirth.HasValue) student.DateOfBirth = dto.DateOfBirth.Value;
        if (dto.Gender != null) student.Gender = dto.Gender;
        if (dto.Address != null) student.Address = dto.Address;
        if (dto.MedicalNotes != null) student.MedicalNotes = dto.MedicalNotes;

        await _db.SaveChangesAsync();
        return await MapToResponseAsync(id);
    }

    // ── Delete (soft) ─────────────────────────────────────────────────────────
    public async Task DeleteAsync(Guid id)
    {
        var student = await _db.Students.FindAsync(id)
            ?? throw new KeyNotFoundException($"Student {id} not found.");
        student.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Assign Class ──────────────────────────────────────────────────────────
    public async Task<StudentResponseDto> AssignClassAsync(Guid studentId, Guid classId)
    {
        var student = await _db.Students.FindAsync(studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        var classExists = await _db.Classes.AnyAsync(c => c.Id == classId);
        if (!classExists) throw new KeyNotFoundException($"Class {classId} not found.");

        student.ClassId = classId;
        await _db.SaveChangesAsync();
        return await MapToResponseAsync(studentId);
    }

    // ── Transfer ──────────────────────────────────────────────────────────────
    public async Task<StudentResponseDto> TransferAsync(Guid studentId, TransferStudentDto dto)
    {
        var student = await _db.Students.FindAsync(studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        var newClass = await _db.Classes.FindAsync(dto.NewClassId)
            ?? throw new KeyNotFoundException($"Class {dto.NewClassId} not found.");

        student.ClassId = dto.NewClassId;
        student.Status = "Active";
        await _db.SaveChangesAsync();
        return await MapToResponseAsync(studentId);
    }

    // ── Update Status ─────────────────────────────────────────────────────────
    public async Task<StudentResponseDto> UpdateStatusAsync(Guid studentId, string status)
    {
        var allowed = new[] { "Active", "Inactive", "Transferred", "Graduated", "Withdrawn" };
        if (!allowed.Contains(status))
            throw new ArgumentException($"Invalid status. Allowed: {string.Join(", ", allowed)}");

        var student = await _db.Students.FindAsync(studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        student.Status = status;
        await _db.SaveChangesAsync();
        return await MapToResponseAsync(studentId);
    }

    // ── Upload Photo ──────────────────────────────────────────────────────────
    public async Task<string> UploadPhotoAsync(Guid studentId, IFormFile file)
    {
        var student = await _db.Students.FindAsync(studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        ValidateImage(file);

        // Delete old photo if exists
        if (!string.IsNullOrEmpty(student.PhotoUrl))
            await _fileStorage.DeleteFileAsync(student.PhotoUrl);

        var relativePath = await _fileStorage.SaveFileAsync(file, $"students/{studentId}/photos");
        student.PhotoUrl = relativePath;
        await _db.SaveChangesAsync();

        return _fileStorage.GetFileUrl(relativePath);
    }

    // ── Upload Document ───────────────────────────────────────────────────────
    public async Task<StudentDocumentResponseDto> UploadDocumentAsync(Guid studentId, UploadDocumentDto dto)
    {
        var student = await _db.Students.FindAsync(studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        ValidateDocument(dto.File);

        var relativePath = await _fileStorage.SaveFileAsync(dto.File, $"students/{studentId}/documents");

        var document = new StudentDocument
        {
            StudentId = studentId,
            Name = dto.Name,
            FileUrl = relativePath,
            FileType = Path.GetExtension(dto.File.FileName).ToLowerInvariant().TrimStart('.'),
            FileSizeBytes = dto.File.Length
        };

        _db.StudentDocuments.Add(document);
        await _db.SaveChangesAsync();

        return MapDocumentToDto(document);
    }

    // ── Delete Document ───────────────────────────────────────────────────────
    public async Task DeleteDocumentAsync(Guid documentId)
    {
        var doc = await _db.StudentDocuments.FindAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        await _fileStorage.DeleteFileAsync(doc.FileUrl);
        _db.StudentDocuments.Remove(doc);
        await _db.SaveChangesAsync();
    }

    // ── Get Documents ─────────────────────────────────────────────────────────
    public async Task<IEnumerable<StudentDocumentResponseDto>> GetDocumentsAsync(Guid studentId)
    {
        var docs = await _db.StudentDocuments
            .Where(d => d.StudentId == studentId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        return docs.Select(MapDocumentToDto);
    }

    // ── Bulk Import ───────────────────────────────────────────────────────────
    public async Task<BulkImportResultDto> BulkImportAsync(IFormFile csvFile)
    {
        var result = new BulkImportResultDto();

        using var reader = new StreamReader(csvFile.OpenReadStream());
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
        };
        using var csv = new CsvReader(reader, config);

        var rows = csv.GetRecords<StudentCsvRow>().ToList();
        result.TotalRows = rows.Count;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNum = i + 2; // 1-indexed + header row

            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(row.FirstName))
                { result.Errors.Add(new BulkImportErrorDto { Row = rowNum, Field = "FirstName", Message = "Required" }); result.Failed++; continue; }

                if (string.IsNullOrWhiteSpace(row.LastName))
                { result.Errors.Add(new BulkImportErrorDto { Row = rowNum, Field = "LastName", Message = "Required" }); result.Failed++; continue; }

                var dobFormats = new[] { "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy" };
                if (!DateTime.TryParseExact(row.DateOfBirth?.Trim(), dobFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
                { result.Errors.Add(new BulkImportErrorDto { Row = rowNum, Field = "DateOfBirth", Message = "Invalid format. Use dd/MM/yyyy (e.g. 05/11/2009)" }); result.Failed++; continue; }

                if (string.IsNullOrWhiteSpace(row.GuardianEmail))
                { result.Errors.Add(new BulkImportErrorDto { Row = rowNum, Field = "GuardianEmail", Message = "Required" }); result.Failed++; continue; }

                // Resolve class
                Guid? classId = null;
                if (!string.IsNullOrWhiteSpace(row.ClassName))
                {
                    var cls = await _db.Classes.FirstOrDefaultAsync(c => c.Name == row.ClassName);
                    if (cls != null) classId = cls.Id;
                }

                // Find or create guardian — re-run safe: check by email before creating
                var guardianEmail = row.GuardianEmail.ToLower().Trim();
                var guardian = await _db.Guardians.FirstOrDefaultAsync(g => g.Email == guardianEmail);
                if (guardian == null)
                {
                    // Guard against a partial-failure re-run where the guardian User
                    // was already committed but the Guardian row was not
                    var existingGuardianUser = await _db.Users
                        .FirstOrDefaultAsync(u => u.Email == guardianEmail);

                    guardian = await CreateGuardianInternalAsync(new CreateGuardianDto
                    {
                        FirstName = row.GuardianFirstName,
                        LastName = row.GuardianLastName,
                        Email = row.GuardianEmail,
                        PhoneNumber = row.GuardianPhone,
                        Relationship = row.GuardianRelationship
                    }, existingGuardianUser);
                }

                var studentUser = new User
                {
                    Email = GenerateStudentEmail(row.FirstName, row.LastName),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerateTempPassword()),
                    Role = "Student",
                    IsActive = true
                };
                _db.Users.Add(studentUser);
                await _db.SaveChangesAsync();

                var student = new Student
                {
                    StudentNumber = await _numberGen.GenerateAsync(),
                    FirstName = row.FirstName.Trim(),
                    LastName = row.LastName.Trim(),
                    DateOfBirth = dob,
                    Gender = row.Gender ?? "Unspecified",
                    ClassId = classId,
                    UserId = studentUser.Id,
                    GuardianId = guardian.Id,
                    Status = "Active"
                };

                _db.Students.Add(student);
                await _db.SaveChangesAsync();
                result.Succeeded++;
            }
            catch (Exception ex)
            {
                // Clear tracked entities so a failed row doesn't poison subsequent rows
                _db.ChangeTracker.Clear();
                var detail = ex.InnerException?.Message ?? ex.Message;
                result.Errors.Add(new BulkImportErrorDto { Row = rowNum, Field = "General", Message = detail });
                result.Failed++;
            }
        }

        return result;
    }

    // ── ID Card (placeholder — full PDF in Sprint 4) ──────────────────────────
    public async Task<byte[]> GenerateStudentIdCardAsync(Guid studentId)
    {
        var student = await _db.Students
            .Include(s => s.Class)
            .Include(s => s.Guardian)
            .FirstOrDefaultAsync(s => s.Id == studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        // Simple text-based placeholder — full styled PDF in Phase 4
        var content = $"""
            SCHOOL MANAGEMENT SYSTEM
            ========================
            STUDENT ID CARD

            Name:           {student.FirstName} {student.LastName}
            Student No:     {student.StudentNumber}
            Class:          {student.Class?.Name ?? "Unassigned"}
            Gender:         {student.Gender}
            Date of Birth:  {student.DateOfBirth:dd/MM/yyyy}
            Status:         {student.Status}

            Guardian:       {student.Guardian.FirstName} {student.Guardian.LastName}
            Contact:        {student.Guardian.PhoneNumber}
            """;

        return System.Text.Encoding.UTF8.GetBytes(content);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task<Guardian> CreateGuardianInternalAsync(CreateGuardianDto dto, User? existingUser = null)
    {
        User guardianUser;
        if (existingUser != null)
        {
            guardianUser = existingUser;
        }
        else
        {
            guardianUser = new User
            {
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(GenerateTempPassword()),
                Role = "Parent",
                IsActive = true
            };
            _db.Users.Add(guardianUser);
            await _db.SaveChangesAsync();
        }

        var guardian = new Guardian
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.ToLower().Trim(),
            PhoneNumber = dto.PhoneNumber,
            AlternatePhone = dto.AlternatePhone,
            Relationship = dto.Relationship,
            Address = dto.Address,
            Occupation = dto.Occupation,
            UserId = guardianUser.Id
        };

        _db.Guardians.Add(guardian);
        await _db.SaveChangesAsync();
        return guardian;
    }

    private async Task<StudentResponseDto> MapToResponseAsync(Guid studentId)
    {
        var student = await _db.Students
            .Include(s => s.Class).ThenInclude(c => c!.AcademicYear)
            .Include(s => s.Guardian)
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Id == studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        return MapToDto(student);
    }

    private static StudentResponseDto MapToDto(Student student)
    {
        return new StudentResponseDto
        {
            Id = student.Id,
            StudentNumber = student.StudentNumber,
            FirstName = student.FirstName,
            LastName = student.LastName,
            DateOfBirth = student.DateOfBirth,
            Gender = student.Gender,
            PhotoUrl = student.PhotoUrl,
            Status = student.Status,
            Address = student.Address,
            MedicalNotes = student.MedicalNotes,
            ClassId = student.ClassId,
            ClassName = student.Class?.Name,
            AcademicYear = student.Class?.AcademicYear?.Name,
            Guardian = student.Guardian == null ? null : new GuardianSummaryDto
            {
                Id = student.Guardian.Id,
                FullName = $"{student.Guardian.FirstName} {student.Guardian.LastName}",
                Email = student.Guardian.Email,
                PhoneNumber = student.Guardian.PhoneNumber,
                Relationship = student.Guardian.Relationship
            },
            Documents = student.Documents.Select(MapDocumentToDto).ToList(),
            CreatedAt = student.CreatedAt
        };
    }

    private static StudentDocumentResponseDto MapDocumentToDto(StudentDocument doc) => new()
    {
        Id = doc.Id,
        Name = doc.Name,
        FileUrl = doc.FileUrl,
        FileType = doc.FileType,
        FileSizeBytes = doc.FileSizeBytes,
        CreatedAt = doc.CreatedAt
    };

    private static void ValidateImage(IFormFile file)
    {
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new ArgumentException("Photo must be JPG, PNG, or WEBP.");
        if (file.Length > 5 * 1024 * 1024)
            throw new ArgumentException("Photo must be under 5MB.");
    }

    private static void ValidateDocument(IFormFile file)
    {
        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new ArgumentException("Document must be PDF, image, or Word document.");
        if (file.Length > 10 * 1024 * 1024)
            throw new ArgumentException("Document must be under 10MB.");
    }

    private static string GenerateTempPassword() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));

    private static string GenerateStudentEmail(string firstName, string lastName) =>
        $"{firstName.ToLower().Trim()}.{lastName.ToLower().Trim()}.{Guid.NewGuid().ToString()[..4]}@student.school.com";
}