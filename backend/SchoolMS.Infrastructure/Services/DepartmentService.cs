using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Academic;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _db;

    public DepartmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto)
    {
        var exists = await _db.Departments.AnyAsync(d => d.Name == dto.Name);
        if (exists)
            throw new InvalidOperationException($"Department '{dto.Name}' already exists.");

        var dept = new Department
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            HeadOfDepartmentId = dto.HeadOfDepartmentId
        };

        _db.Departments.Add(dept);
        await _db.SaveChangesAsync();
        return await MapAsync(dept.Id);
    }

    public async Task<DepartmentResponseDto> GetByIdAsync(Guid id)
    {
        var exists = await _db.Departments.AnyAsync(d => d.Id == id);
        if (!exists) throw new KeyNotFoundException($"Department {id} not found.");
        return await MapAsync(id);
    }

    public async Task<IEnumerable<DepartmentResponseDto>> GetAllAsync()
    {
        var depts = await _db.Departments
            .Include(d => d.HeadOfDepartment)
            .Include(d => d.Staff)
            .Include(d => d.Subjects)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return depts.Select(d => new DepartmentResponseDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            HeadOfDepartmentId = d.HeadOfDepartmentId,
            HeadOfDepartmentName = d.HeadOfDepartment != null
                ? $"{d.HeadOfDepartment.FirstName} {d.HeadOfDepartment.LastName}"
                : null,
            StaffCount = d.Staff.Count,
            SubjectCount = d.Subjects.Count,
            CreatedAt = d.CreatedAt
        });
    }

    public async Task<DepartmentResponseDto> UpdateAsync(Guid id, UpdateDepartmentDto dto)
    {
        var dept = await _db.Departments.FindAsync(id)
            ?? throw new KeyNotFoundException($"Department {id} not found.");

        if (dto.Name != null) dept.Name = dto.Name.Trim();
        if (dto.Description != null) dept.Description = dto.Description;
        if (dto.HeadOfDepartmentId.HasValue) dept.HeadOfDepartmentId = dto.HeadOfDepartmentId;

        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var dept = await _db.Departments
            .Include(d => d.Staff)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"Department {id} not found.");

        if (dept.Staff.Any(s => s.Status == "Active"))
            throw new InvalidOperationException("Cannot delete a department with active staff.");

        dept.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    private async Task<DepartmentResponseDto> MapAsync(Guid id)
    {
        var d = await _db.Departments
            .Include(d => d.HeadOfDepartment)
            .Include(d => d.Staff)
            .Include(d => d.Subjects)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException();

        return new DepartmentResponseDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            HeadOfDepartmentId = d.HeadOfDepartmentId,
            HeadOfDepartmentName = d.HeadOfDepartment != null
                ? $"{d.HeadOfDepartment.FirstName} {d.HeadOfDepartment.LastName}"
                : null,
            StaffCount = d.Staff.Count,
            SubjectCount = d.Subjects.Count,
            CreatedAt = d.CreatedAt
        };
    }
}