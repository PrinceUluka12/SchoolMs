using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Academic;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class ClassService : IClassService
{
    private readonly AppDbContext _db;

    public ClassService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ClassResponseDto> CreateAsync(CreateClassDto dto)
    {
        var yearExists = await _db.AcademicYears.AnyAsync(a => a.Id == dto.AcademicYearId);
        if (!yearExists)
            throw new KeyNotFoundException($"Academic year {dto.AcademicYearId} not found.");

        var cls = new Class
        {
            Name = dto.Name.Trim(),
            Section = dto.Section?.Trim(),
            Stream = dto.Stream?.Trim(),
            Level = dto.Level,
            Capacity = dto.Capacity,
            AcademicYearId = dto.AcademicYearId,
            ClassTeacherId = dto.ClassTeacherId
        };

        _db.Classes.Add(cls);
        await _db.SaveChangesAsync();
        return await MapAsync(cls.Id);
    }

    public async Task<ClassResponseDto> GetByIdAsync(Guid id)
    {
        var exists = await _db.Classes.AnyAsync(c => c.Id == id);
        if (!exists) throw new KeyNotFoundException($"Class {id} not found.");
        return await MapAsync(id);
    }

    public async Task<IEnumerable<ClassResponseDto>> GetAllAsync(Guid? academicYearId)
    {
        var query = _db.Classes
            .Include(c => c.AcademicYear)
            .Include(c => c.ClassTeacher)
            .Include(c => c.Students)
            .Include(c => c.ClassSubjects).ThenInclude(cs => cs.Subject)
            .Include(c => c.ClassSubjects).ThenInclude(cs => cs.Teacher)
            .AsQueryable();

        if (academicYearId.HasValue)
            query = query.Where(c => c.AcademicYearId == academicYearId);

        var classes = await query.OrderBy(c => c.Level).ThenBy(c => c.Name).ToListAsync();
        return classes.Select(MapToDto);
    }

    public async Task<ClassResponseDto> UpdateAsync(Guid id, UpdateClassDto dto)
    {
        var cls = await _db.Classes.FindAsync(id)
            ?? throw new KeyNotFoundException($"Class {id} not found.");

        if (dto.Name != null) cls.Name = dto.Name.Trim();
        if (dto.Section != null) cls.Section = dto.Section.Trim();
        if (dto.Stream != null) cls.Stream = dto.Stream.Trim();
        if (dto.Level.HasValue) cls.Level = dto.Level.Value;
        if (dto.Capacity.HasValue) cls.Capacity = dto.Capacity.Value;
        if (dto.ClassTeacherId.HasValue) cls.ClassTeacherId = dto.ClassTeacherId;

        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var cls = await _db.Classes
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Class {id} not found.");

        if (cls.Students.Any(s => s.Status == "Active"))
            throw new InvalidOperationException("Cannot delete a class with active students.");

        cls.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<ClassResponseDto> AssignTeacherAsync(Guid classId, Guid teacherId)
    {
        var cls = await _db.Classes.FindAsync(classId)
            ?? throw new KeyNotFoundException($"Class {classId} not found.");

        var teacher = await _db.Staff.FindAsync(teacherId)
            ?? throw new KeyNotFoundException($"Staff {teacherId} not found.");

        cls.ClassTeacherId = teacherId;
        await _db.SaveChangesAsync();
        return await MapAsync(classId);
    }

    public async Task<ClassSubjectResponseDto> AssignSubjectAsync(Guid classId, AssignSubjectDto dto)
    {
        var classExists = await _db.Classes.AnyAsync(c => c.Id == classId);
        if (!classExists) throw new KeyNotFoundException($"Class {classId} not found.");

        var subjectExists = await _db.Subjects.AnyAsync(s => s.Id == dto.SubjectId);
        if (!subjectExists) throw new KeyNotFoundException($"Subject {dto.SubjectId} not found.");

        var existing = await _db.ClassSubjects
            .FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.SubjectId == dto.SubjectId);

        if (existing != null)
        {
            // Re-activate and update teacher if already exists
            existing.IsActive = true;
            existing.TeacherId = dto.TeacherId;
            await _db.SaveChangesAsync();
            return await MapClassSubjectAsync(existing.Id);
        }

        var cs = new ClassSubject
        {
            ClassId = classId,
            SubjectId = dto.SubjectId,
            TeacherId = dto.TeacherId,
            IsActive = true
        };

        _db.ClassSubjects.Add(cs);
        await _db.SaveChangesAsync();
        return await MapClassSubjectAsync(cs.Id);
    }

    public async Task RemoveSubjectAsync(Guid classId, Guid subjectId)
    {
        var cs = await _db.ClassSubjects
            .FirstOrDefaultAsync(cs => cs.ClassId == classId && cs.SubjectId == subjectId)
            ?? throw new KeyNotFoundException("Class-subject assignment not found.");

        cs.IsActive = false;
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ClassSubjectResponseDto>> GetSubjectsAsync(Guid classId)
    {
        var subjects = await _db.ClassSubjects
            .Include(cs => cs.Subject)
            .Include(cs => cs.Teacher)
            .Where(cs => cs.ClassId == classId && cs.IsActive)
            .ToListAsync();

        return subjects.Select(cs => new ClassSubjectResponseDto
        {
            Id = cs.Id,
            SubjectId = cs.SubjectId,
            SubjectName = cs.Subject.Name,
            SubjectCode = cs.Subject.Code,
            TeacherId = cs.TeacherId,
            TeacherName = cs.Teacher != null
                ? $"{cs.Teacher.FirstName} {cs.Teacher.LastName}"
                : null,
            IsActive = cs.IsActive
        });
    }

    private async Task<ClassResponseDto> MapAsync(Guid id)
    {
        var cls = await _db.Classes
            .Include(c => c.AcademicYear)
            .Include(c => c.ClassTeacher)
            .Include(c => c.Students)
            .Include(c => c.ClassSubjects).ThenInclude(cs => cs.Subject)
            .Include(c => c.ClassSubjects).ThenInclude(cs => cs.Teacher)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException();
        return MapToDto(cls);
    }

    private static ClassResponseDto MapToDto(Class c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Section = c.Section,
        Stream = c.Stream,
        Level = c.Level,
        Capacity = c.Capacity,
        AcademicYearId = c.AcademicYearId,
        AcademicYearName = c.AcademicYear?.Name ?? "",
        ClassTeacherId = c.ClassTeacherId,
        ClassTeacherName = c.ClassTeacher != null
            ? $"{c.ClassTeacher.FirstName} {c.ClassTeacher.LastName}"
            : null,
        StudentCount = c.Students.Count(s => s.Status == "Active"),
        SubjectCount = c.ClassSubjects.Count(cs => cs.IsActive),
        Subjects = c.ClassSubjects.Where(cs => cs.IsActive).Select(cs => new ClassSubjectResponseDto
        {
            Id = cs.Id,
            SubjectId = cs.SubjectId,
            SubjectName = cs.Subject?.Name ?? "",
            SubjectCode = cs.Subject?.Code ?? "",
            TeacherId = cs.TeacherId,
            TeacherName = cs.Teacher != null
                ? $"{cs.Teacher.FirstName} {cs.Teacher.LastName}"
                : null,
            IsActive = cs.IsActive
        }).ToList(),
        CreatedAt = c.CreatedAt
    };

    private async Task<ClassSubjectResponseDto> MapClassSubjectAsync(Guid id)
    {
        var cs = await _db.ClassSubjects
            .Include(x => x.Subject)
            .Include(x => x.Teacher)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();

        return new ClassSubjectResponseDto
        {
            Id = cs.Id,
            SubjectId = cs.SubjectId,
            SubjectName = cs.Subject.Name,
            SubjectCode = cs.Subject.Code,
            TeacherId = cs.TeacherId,
            TeacherName = cs.Teacher != null
                ? $"{cs.Teacher.FirstName} {cs.Teacher.LastName}"
                : null,
            IsActive = cs.IsActive
        };
    }
}