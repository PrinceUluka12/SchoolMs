using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Academic;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class AcademicYearService : IAcademicYearService
{
    private readonly AppDbContext _db;

    public AcademicYearService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AcademicYearResponseDto> CreateAsync(CreateAcademicYearDto dto)
    {
        if (dto.EndDate <= dto.StartDate)
            throw new ArgumentException("End date must be after start date.");

        var exists = await _db.AcademicYears.AnyAsync(a => a.Name == dto.Name);
        if (exists)
            throw new InvalidOperationException($"Academic year '{dto.Name}' already exists.");

        var year = new AcademicYear
        {
            Name = dto.Name.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = "Upcoming",
            IsCurrent = false
        };

        _db.AcademicYears.Add(year);
        await _db.SaveChangesAsync();
        return await MapAsync(year.Id);
    }

    public async Task<AcademicYearResponseDto> GetByIdAsync(Guid id)
    {
        var exists = await _db.AcademicYears.AnyAsync(a => a.Id == id);
        if (!exists) throw new KeyNotFoundException($"Academic year {id} not found.");
        return await MapAsync(id);
    }

    public async Task<IEnumerable<AcademicYearResponseDto>> GetAllAsync()
    {
        var years = await _db.AcademicYears
            .Include(a => a.Terms)
            .Include(a => a.Classes)
            .OrderByDescending(a => a.StartDate)
            .ToListAsync();

        return years.Select(MapToDto);
    }

    public async Task<AcademicYearResponseDto> UpdateAsync(Guid id, UpdateAcademicYearDto dto)
    {
        var year = await _db.AcademicYears.FindAsync(id)
            ?? throw new KeyNotFoundException($"Academic year {id} not found.");

        if (dto.Name != null) year.Name = dto.Name.Trim();
        if (dto.StartDate.HasValue) year.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) year.EndDate = dto.EndDate.Value;
        if (dto.Status != null) year.Status = dto.Status;

        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var year = await _db.AcademicYears
            .Include(a => a.Classes)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException($"Academic year {id} not found.");

        if (year.IsCurrent)
            throw new InvalidOperationException("Cannot delete the current academic year.");

        if (year.Classes.Any())
            throw new InvalidOperationException("Cannot delete an academic year that has classes.");

        year.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<AcademicYearResponseDto> SetCurrentAsync(Guid id)
    {
        // Unset all current flags
        var allYears = await _db.AcademicYears.ToListAsync();
        foreach (var y in allYears)
        {
            y.IsCurrent = false;
            y.Status = y.EndDate < DateTime.UtcNow ? "Closed" : "Upcoming";
        }

        var target = allYears.FirstOrDefault(y => y.Id == id)
            ?? throw new KeyNotFoundException($"Academic year {id} not found.");

        target.IsCurrent = true;
        target.Status = "Active";
        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    private async Task<AcademicYearResponseDto> MapAsync(Guid id)
    {
        var y = await _db.AcademicYears
            .Include(a => a.Terms)
            .Include(a => a.Classes)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new KeyNotFoundException();
        return MapToDto(y);
    }

    private static AcademicYearResponseDto MapToDto(AcademicYear y) => new()
    {
        Id = y.Id,
        Name = y.Name,
        StartDate = y.StartDate,
        EndDate = y.EndDate,
        IsCurrent = y.IsCurrent,
        Status = y.Status,
        TermCount = y.Terms.Count,
        ClassCount = y.Classes.Count,
        Terms = y.Terms.OrderBy(t => t.TermNumber).Select(t => new TermResponseDto
        {
            Id = t.Id,
            Name = t.Name,
            TermNumber = t.TermNumber,
            StartDate = t.StartDate,
            EndDate = t.EndDate,
            IsCurrent = t.IsCurrent,
            Status = t.Status,
            AcademicYearId = t.AcademicYearId,
            AcademicYearName = y.Name,
            CreatedAt = t.CreatedAt
        }).ToList(),
        CreatedAt = y.CreatedAt
    };
}