using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Dashboard;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;
using SchoolMS.Infrastructure.Services;

namespace SchoolMS.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly RedisCacheService _cache;
    private const string CacheKey = "dashboard:stats";

    public DashboardService(AppDbContext db, RedisCacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        // Return cached result if available (5-minute TTL)
        var cached = await _cache.GetAsync<DashboardStatsDto>(CacheKey);
        if (cached != null) return cached;

        var currentYear = await _db.AcademicYears
            .FirstOrDefaultAsync(a => a.IsCurrent);

        var currentTerm = await _db.Terms
            .FirstOrDefaultAsync(t => t.IsCurrent);

        var totalStudents  = await _db.Students.CountAsync();
        var activeStudents = await _db.Students.CountAsync(s => s.Status == "Active");
        var totalStaff     = await _db.Staff.CountAsync();
        var activeStaff    = await _db.Staff.CountAsync(s => s.Status == "Active");
        var totalClasses   = await _db.Classes.CountAsync(c =>
            currentYear == null || c.AcademicYearId == currentYear.Id);

        var studentsByGender = await _db.Students
            .Where(s => s.Status == "Active")
            .GroupBy(s => s.Gender)
            .Select(g => new CountByLabelDto { Label = g.Key, Count = g.Count() })
            .ToListAsync();

        var studentsByClass = await _db.Students
            .Where(s => s.Status == "Active" && s.ClassId != null)
            .Include(s => s.Class)
            .GroupBy(s => s.Class!.Name)
            .Select(g => new CountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(8)
            .ToListAsync();

        var staffByRole = await _db.Staff
            .Where(s => s.Status == "Active")
            .GroupBy(s => s.Role)
            .Select(g => new CountByLabelDto { Label = g.Key, Count = g.Count() })
            .ToListAsync();

        var recentStudents = await _db.Students
            .Include(s => s.Class)
            .OrderByDescending(s => s.CreatedAt)
            .Take(5)
            .Select(s => new RecentStudentDto
            {
                Id = s.Id,
                FullName = $"{s.FirstName} {s.LastName}",
                StudentNumber = s.StudentNumber,
                ClassName = s.Class != null ? s.Class.Name : null,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        var result = new DashboardStatsDto
        {
            TotalStudents      = totalStudents,
            ActiveStudents     = activeStudents,
            TotalStaff         = totalStaff,
            ActiveStaff        = activeStaff,
            TotalClasses       = totalClasses,
            CurrentAcademicYear = currentYear?.Name,
            CurrentTerm        = currentTerm?.Name,
            StudentsByGender   = studentsByGender,
            StudentsByClass    = studentsByClass,
            StaffByRole        = staffByRole,
            RecentStudents     = recentStudents
        };

        // Cache for 5 minutes
        await _cache.SetAsync(CacheKey, result, TimeSpan.FromMinutes(5));
        return result;
    }
}