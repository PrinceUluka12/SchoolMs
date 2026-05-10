using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Academic;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class SubjectService : ISubjectService
{
    private readonly AppDbContext _db;

    public SubjectService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SubjectResponseDto> CreateAsync(CreateSubjectDto dto)
    {
        var exists = await _db.Subjects.AnyAsync(s => s.Code == dto.Code.ToUpper());
        if (exists)
            throw new InvalidOperationException($"Subject code '{dto.Code}' already exists.");

        var subject = new Subject
        {
            Name = dto.Name.Trim(),
            Code = dto.Code.ToUpper().Trim(),
            Type = dto.Type,
            CreditHours = dto.CreditHours,
            Description = dto.Description,
            DepartmentId = dto.DepartmentId,
            IsActive = true
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();
        return await MapAsync(subject.Id);
    }

    public async Task<SubjectResponseDto> GetByIdAsync(Guid id)
    {
        var exists = await _db.Subjects.AnyAsync(s => s.Id == id);
        if (!exists) throw new KeyNotFoundException($"Subject {id} not found.");
        return await MapAsync(id);
    }

    public async Task<IEnumerable<SubjectResponseDto>> GetAllAsync()
    {
        var subjects = await _db.Subjects
            .Include(s => s.Department)
            .Include(s => s.ClassSubjects)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return subjects.Select(MapToDto);
    }

    public async Task<SubjectResponseDto> UpdateAsync(Guid id, UpdateSubjectDto dto)
    {
        var subject = await _db.Subjects.FindAsync(id)
            ?? throw new KeyNotFoundException($"Subject {id} not found.");

        if (dto.Name != null) subject.Name = dto.Name.Trim();
        if (dto.Code != null) subject.Code = dto.Code.ToUpper().Trim();
        if (dto.Type != null) subject.Type = dto.Type;
        if (dto.CreditHours.HasValue) subject.CreditHours = dto.CreditHours.Value;
        if (dto.Description != null) subject.Description = dto.Description;
        if (dto.DepartmentId.HasValue) subject.DepartmentId = dto.DepartmentId;
        if (dto.IsActive.HasValue) subject.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var subject = await _db.Subjects
            .Include(s => s.ClassSubjects)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Subject {id} not found.");

        if (subject.ClassSubjects.Any(cs => cs.IsActive))
            throw new InvalidOperationException("Cannot delete a subject currently assigned to classes.");

        subject.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private async Task<SubjectResponseDto> MapAsync(Guid id)
    {
        var s = await _db.Subjects
            .Include(x => x.Department)
            .Include(x => x.ClassSubjects)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapToDto(s);
    }

    private static SubjectResponseDto MapToDto(Subject s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Code = s.Code,
        Type = s.Type,
        CreditHours = s.CreditHours,
        Description = s.Description,
        IsActive = s.IsActive,
        DepartmentId = s.DepartmentId,
        DepartmentName = s.Department?.Name,
        ClassCount = s.ClassSubjects.Count(cs => cs.IsActive),
        CreatedAt = s.CreatedAt
    };
}