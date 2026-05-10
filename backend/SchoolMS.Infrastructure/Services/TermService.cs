using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Academic;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class TermService : ITermService
{
    private readonly AppDbContext _db;

    public TermService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TermResponseDto> CreateAsync(CreateTermDto dto)
    {
        var year = await _db.AcademicYears.FindAsync(dto.AcademicYearId)
            ?? throw new KeyNotFoundException($"Academic year {dto.AcademicYearId} not found.");

        if (dto.EndDate <= dto.StartDate)
            throw new ArgumentException("End date must be after start date.");

        var exists = await _db.Terms.AnyAsync(t =>
            t.AcademicYearId == dto.AcademicYearId && t.TermNumber == dto.TermNumber);
        if (exists)
            throw new InvalidOperationException($"Term {dto.TermNumber} already exists for this academic year.");

        var term = new Term
        {
            Name = dto.Name.Trim(),
            TermNumber = dto.TermNumber,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            AcademicYearId = dto.AcademicYearId,
            Status = "Upcoming",
            IsCurrent = false
        };

        _db.Terms.Add(term);
        await _db.SaveChangesAsync();
        return await MapAsync(term.Id);
    }

    public async Task<TermResponseDto> GetByIdAsync(Guid id)
    {
        var exists = await _db.Terms.AnyAsync(t => t.Id == id);
        if (!exists) throw new KeyNotFoundException($"Term {id} not found.");
        return await MapAsync(id);
    }

    public async Task<IEnumerable<TermResponseDto>> GetByAcademicYearAsync(Guid academicYearId)
    {
        var terms = await _db.Terms
            .Include(t => t.AcademicYear)
            .Where(t => t.AcademicYearId == academicYearId)
            .OrderBy(t => t.TermNumber)
            .ToListAsync();

        return terms.Select(MapToDto);
    }

    public async Task<TermResponseDto> UpdateAsync(Guid id, UpdateTermDto dto)
    {
        var term = await _db.Terms.FindAsync(id)
            ?? throw new KeyNotFoundException($"Term {id} not found.");

        if (dto.Name != null) term.Name = dto.Name.Trim();
        if (dto.StartDate.HasValue) term.StartDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) term.EndDate = dto.EndDate.Value;
        if (dto.Status != null) term.Status = dto.Status;

        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var term = await _db.Terms.FindAsync(id)
            ?? throw new KeyNotFoundException($"Term {id} not found.");

        if (term.IsCurrent)
            throw new InvalidOperationException("Cannot delete the current term.");

        term.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<TermResponseDto> SetCurrentAsync(Guid id)
    {
        var allTerms = await _db.Terms.ToListAsync();
        foreach (var t in allTerms)
        {
            t.IsCurrent = false;
            t.Status = t.EndDate < DateTime.UtcNow ? "Closed" : "Upcoming";
        }

        var target = allTerms.FirstOrDefault(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Term {id} not found.");

        target.IsCurrent = true;
        target.Status = "Active";
        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    public async Task<TermResponseDto?> GetCurrentAsync()
    {
        var term = await _db.Terms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.IsCurrent);
        return term == null ? null : MapToDto(term);
    }

    private async Task<TermResponseDto> MapAsync(Guid id)
    {
        var t = await _db.Terms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException();
        return MapToDto(t);
    }

    private static TermResponseDto MapToDto(Term t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        TermNumber = t.TermNumber,
        StartDate = t.StartDate,
        EndDate = t.EndDate,
        IsCurrent = t.IsCurrent,
        Status = t.Status,
        AcademicYearId = t.AcademicYearId,
        AcademicYearName = t.AcademicYear?.Name ?? "",
        CreatedAt = t.CreatedAt
    };
}