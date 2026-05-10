using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Common;
using SchoolMS.Core.DTOs.Staff;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class StaffService : IStaffService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly StaffNumberGenerator _numberGen;

    public StaffService(AppDbContext db, IFileStorageService fileStorage, StaffNumberGenerator numberGen)
    {
        _db = db;
        _fileStorage = fileStorage;
        _numberGen = numberGen;
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<StaffResponseDto> CreateAsync(CreateStaffDto dto)
    {
        var emailExists = await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower());
        if (emailExists)
            throw new InvalidOperationException($"A user with email {dto.Email} already exists.");

        var tempPassword = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(12));

        var user = new User
        {
            Email = dto.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            Role = dto.Role,
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var staffNumber = await _numberGen.GenerateAsync();

        var staff = new Staff
        {
            StaffNumber = staffNumber,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.ToLower().Trim(),
            PhoneNumber = dto.PhoneNumber,
            Gender = dto.Gender,
            DateOfBirth = dto.DateOfBirth,
            Role = dto.Role,
            ContractType = dto.ContractType,
            JoinDate = dto.JoinDate,
            DepartmentId = dto.DepartmentId,
            Qualifications = dto.Qualifications,
            Address = dto.Address,
            UserId = user.Id,
            Status = "Active"
        };

        _db.Staff.Add(staff);
        await _db.SaveChangesAsync();
        return await MapToResponseAsync(staff.Id);
    }

    // ── Get By Id ─────────────────────────────────────────────────────────────
    public async Task<StaffResponseDto> GetByIdAsync(Guid id)
    {
        var exists = await _db.Staff.AnyAsync(s => s.Id == id);
        if (!exists) throw new KeyNotFoundException($"Staff {id} not found.");
        return await MapToResponseAsync(id);
    }

    // ── Get All ───────────────────────────────────────────────────────────────
    public async Task<PagedResult<StaffResponseDto>> GetAllAsync(StaffFilterDto filter)
    {
        var query = _db.Staff
            .Include(s => s.Department)
            .Include(s => s.Documents)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(s) ||
                x.LastName.ToLower().Contains(s) ||
                x.StaffNumber.ToLower().Contains(s) ||
                x.Email.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(filter.Role))
            query = query.Where(x => x.Role == filter.Role);

        if (filter.DepartmentId.HasValue)
            query = query.Where(x => x.DepartmentId == filter.DepartmentId);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(x => x.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.ContractType))
            query = query.Where(x => x.ContractType == filter.ContractType);

        query = filter.SortBy.ToLower() switch
        {
            "firstname" => filter.SortDirection == "desc"
                ? query.OrderByDescending(x => x.FirstName)
                : query.OrderBy(x => x.FirstName),
            "staffnumber" => filter.SortDirection == "desc"
                ? query.OrderByDescending(x => x.StaffNumber)
                : query.OrderBy(x => x.StaffNumber),
            "joindate" => filter.SortDirection == "desc"
                ? query.OrderByDescending(x => x.JoinDate)
                : query.OrderBy(x => x.JoinDate),
            _ => filter.SortDirection == "desc"
                ? query.OrderByDescending(x => x.LastName)
                : query.OrderBy(x => x.LastName)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<StaffResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public async Task<StaffResponseDto> UpdateAsync(Guid id, UpdateStaffDto dto)
    {
        var staff = await _db.Staff.FindAsync(id)
            ?? throw new KeyNotFoundException($"Staff {id} not found.");

        if (dto.FirstName != null) staff.FirstName = dto.FirstName.Trim();
        if (dto.LastName != null) staff.LastName = dto.LastName.Trim();
        if (dto.PhoneNumber != null) staff.PhoneNumber = dto.PhoneNumber;
        if (dto.Gender != null) staff.Gender = dto.Gender;
        if (dto.Role != null)
        {
            staff.Role = dto.Role;
            var user = await _db.Users.FindAsync(staff.UserId);
            if (user != null) user.Role = dto.Role;
        }
        if (dto.ContractType != null) staff.ContractType = dto.ContractType;
        if (dto.DepartmentId.HasValue) staff.DepartmentId = dto.DepartmentId;
        if (dto.Qualifications != null) staff.Qualifications = dto.Qualifications;
        if (dto.Address != null) staff.Address = dto.Address;

        await _db.SaveChangesAsync();
        return await MapToResponseAsync(id);
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task DeleteAsync(Guid id)
    {
        var staff = await _db.Staff.FindAsync(id)
            ?? throw new KeyNotFoundException($"Staff {id} not found.");
        staff.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Update Status ─────────────────────────────────────────────────────────
    public async Task<StaffResponseDto> UpdateStatusAsync(Guid id, string status)
    {
        var allowed = new[] { "Active", "Inactive", "Terminated", "OnLeave" };
        if (!allowed.Contains(status))
            throw new ArgumentException($"Invalid status. Allowed: {string.Join(", ", allowed)}");

        var staff = await _db.Staff.FindAsync(id)
            ?? throw new KeyNotFoundException($"Staff {id} not found.");

        staff.Status = status;
        var user = await _db.Users.FindAsync(staff.UserId);
        if (user != null) user.IsActive = status == "Active";

        await _db.SaveChangesAsync();
        return await MapToResponseAsync(id);
    }

    // ── Upload Photo ──────────────────────────────────────────────────────────
    public async Task<string> UploadPhotoAsync(Guid id, IFormFile file)
    {
        var staff = await _db.Staff.FindAsync(id)
            ?? throw new KeyNotFoundException($"Staff {id} not found.");

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new ArgumentException("Photo must be JPG, PNG, or WEBP.");
        if (file.Length > 5 * 1024 * 1024)
            throw new ArgumentException("Photo must be under 5MB.");

        if (!string.IsNullOrEmpty(staff.PhotoUrl))
            await _fileStorage.DeleteFileAsync(staff.PhotoUrl);

        var relativePath = await _fileStorage.SaveFileAsync(file, $"staff/{id}/photos");
        staff.PhotoUrl = relativePath;
        await _db.SaveChangesAsync();
        return _fileStorage.GetFileUrl(relativePath);
    }

    // ── Upload Document ───────────────────────────────────────────────────────
    public async Task<StaffDocumentResponseDto> UploadDocumentAsync(Guid id, UploadStaffDocumentDto dto)
    {
        var staff = await _db.Staff.FindAsync(id)
            ?? throw new KeyNotFoundException($"Staff {id} not found.");

        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        var ext = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            throw new ArgumentException("Document must be PDF, image, or Word document.");
        if (dto.File.Length > 10 * 1024 * 1024)
            throw new ArgumentException("Document must be under 10MB.");

        var relativePath = await _fileStorage.SaveFileAsync(dto.File, $"staff/{id}/documents");

        var document = new StaffDocument
        {
            StaffId = id,
            Name = dto.Name,
            FileUrl = relativePath,
            FileType = ext.TrimStart('.'),
            FileSizeBytes = dto.File.Length
        };

        _db.StaffDocuments.Add(document);
        await _db.SaveChangesAsync();

        return new StaffDocumentResponseDto
        {
            Id = document.Id,
            Name = document.Name,
            FileUrl = document.FileUrl,
            FileType = document.FileType,
            FileSizeBytes = document.FileSizeBytes,
            CreatedAt = document.CreatedAt
        };
    }

    // ── Delete Document ───────────────────────────────────────────────────────
    public async Task DeleteDocumentAsync(Guid documentId)
    {
        var doc = await _db.StaffDocuments.FindAsync(documentId)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");
        await _fileStorage.DeleteFileAsync(doc.FileUrl);
        _db.StaffDocuments.Remove(doc);
        await _db.SaveChangesAsync();
    }

    // ── Get Documents ─────────────────────────────────────────────────────────
    public async Task<IEnumerable<StaffDocumentResponseDto>> GetDocumentsAsync(Guid staffId)
    {
        var docs = await _db.StaffDocuments
            .Where(d => d.StaffId == staffId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return docs.Select(d => new StaffDocumentResponseDto
        {
            Id = d.Id,
            Name = d.Name,
            FileUrl = d.FileUrl,
            FileType = d.FileType,
            FileSizeBytes = d.FileSizeBytes,
            CreatedAt = d.CreatedAt
        });
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task<StaffResponseDto> MapToResponseAsync(Guid id)
    {
        var staff = await _db.Staff
            .Include(s => s.Department)
            .Include(s => s.Documents)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Staff {id} not found.");
        return MapToDto(staff);
    }

    private static StaffResponseDto MapToDto(Staff s) => new()
    {
        Id = s.Id,
        StaffNumber = s.StaffNumber,
        FirstName = s.FirstName,
        LastName = s.LastName,
        Email = s.Email,
        PhoneNumber = s.PhoneNumber,
        Gender = s.Gender,
        Role = s.Role,
        ContractType = s.ContractType,
        Status = s.Status,
        PhotoUrl = s.PhotoUrl,
        Address = s.Address,
        Qualifications = s.Qualifications,
        JoinDate = s.JoinDate,
        DepartmentId = s.DepartmentId,
        DepartmentName = s.Department?.Name,
        Documents = s.Documents.Select(d => new StaffDocumentResponseDto
        {
            Id = d.Id,
            Name = d.Name,
            FileUrl = d.FileUrl,
            FileType = d.FileType,
            FileSizeBytes = d.FileSizeBytes,
            CreatedAt = d.CreatedAt
        }).ToList(),
        CreatedAt = s.CreatedAt
    };
}