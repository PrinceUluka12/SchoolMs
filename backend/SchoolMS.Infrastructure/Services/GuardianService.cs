using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Students;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class GuardianService : IGuardianService
{
    private readonly AppDbContext _db;

    public GuardianService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GuardianResponseDto> CreateAsync(CreateGuardianDto dto)
    {
        var exists = await _db.Guardians.AnyAsync(g => g.Email == dto.Email.ToLower());
        if (exists) throw new InvalidOperationException($"A guardian with email {dto.Email} already exists.");

        var user = new SchoolMS.Core.Entities.User
        {
            Email = dto.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Convert.ToBase64String(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(12))),
            Role = "Parent",
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

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
            UserId = user.Id
        };

        _db.Guardians.Add(guardian);
        await _db.SaveChangesAsync();
        return await MapAsync(guardian.Id);
    }

    public async Task<GuardianResponseDto> GetByIdAsync(Guid id)
    {
        var exists = await _db.Guardians.AnyAsync(g => g.Id == id);
        if (!exists) throw new KeyNotFoundException($"Guardian {id} not found.");
        return await MapAsync(id);
    }

    public async Task<IEnumerable<GuardianResponseDto>> GetAllAsync()
    {
        var guardians = await _db.Guardians
            .Include(g => g.Students)
            .OrderBy(g => g.LastName)
            .ToListAsync();

        return guardians.Select(g => new GuardianResponseDto
        {
            Id = g.Id,
            FirstName = g.FirstName,
            LastName = g.LastName,
            Email = g.Email,
            PhoneNumber = g.PhoneNumber,
            AlternatePhone = g.AlternatePhone,
            Relationship = g.Relationship,
            Address = g.Address,
            Occupation = g.Occupation,
            StudentCount = g.Students.Count,
            CreatedAt = g.CreatedAt
        });
    }

    public async Task<GuardianResponseDto> UpdateAsync(Guid id, UpdateGuardianDto dto)
    {
        var guardian = await _db.Guardians.FindAsync(id)
            ?? throw new KeyNotFoundException($"Guardian {id} not found.");

        if (dto.FirstName != null) guardian.FirstName = dto.FirstName.Trim();
        if (dto.LastName != null) guardian.LastName = dto.LastName.Trim();
        if (dto.PhoneNumber != null) guardian.PhoneNumber = dto.PhoneNumber;
        if (dto.AlternatePhone != null) guardian.AlternatePhone = dto.AlternatePhone;
        if (dto.Relationship != null) guardian.Relationship = dto.Relationship;
        if (dto.Address != null) guardian.Address = dto.Address;
        if (dto.Occupation != null) guardian.Occupation = dto.Occupation;

        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var guardian = await _db.Guardians
            .Include(g => g.Students)
            .FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new KeyNotFoundException($"Guardian {id} not found.");

        if (guardian.Students.Any(s => s.Status == "Active"))
            throw new InvalidOperationException("Cannot delete a guardian with active students.");

        guardian.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<StudentResponseDto>> GetStudentsByGuardianAsync(Guid guardianId)
    {
        var students = await _db.Students
            .Include(s => s.Class).ThenInclude(c => c!.AcademicYear)
            .Include(s => s.Guardian)
            .Include(s => s.Documents)
            .Where(s => s.GuardianId == guardianId)
            .ToListAsync();

        return students.Select(s => new StudentResponseDto
        {
            Id = s.Id,
            StudentNumber = s.StudentNumber,
            FirstName = s.FirstName,
            LastName = s.LastName,
            DateOfBirth = s.DateOfBirth,
            Gender = s.Gender,
            PhotoUrl = s.PhotoUrl,
            Status = s.Status,
            ClassId = s.ClassId,
            ClassName = s.Class?.Name,
            AcademicYear = s.Class?.AcademicYear?.Name,
            CreatedAt = s.CreatedAt
        });
    }

    private async Task<GuardianResponseDto> MapAsync(Guid id)
    {
        var g = await _db.Guardians
            .Include(g => g.Students)
            .FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new KeyNotFoundException();

        return new GuardianResponseDto
        {
            Id = g.Id,
            FirstName = g.FirstName,
            LastName = g.LastName,
            Email = g.Email,
            PhoneNumber = g.PhoneNumber,
            AlternatePhone = g.AlternatePhone,
            Relationship = g.Relationship,
            Address = g.Address,
            Occupation = g.Occupation,
            StudentCount = g.Students.Count,
            CreatedAt = g.CreatedAt
        };
    }
}