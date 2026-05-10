using Microsoft.EntityFrameworkCore;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class StudentNumberGenerator
{
    private readonly AppDbContext _db;

    public StudentNumberGenerator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync()
    {
        var year = DateTime.UtcNow.Year.ToString();

        // Find the highest existing number for this year
        var prefix = $"STU{year}";
        var last = await _db.Students
            .Where(s => s.StudentNumber.StartsWith(prefix))
            .OrderByDescending(s => s.StudentNumber)
            .Select(s => s.StudentNumber)
            .FirstOrDefaultAsync();

        int next = 1;
        if (last != null)
        {
            var seq = last.Substring(prefix.Length);
            if (int.TryParse(seq, out var parsed))
                next = parsed + 1;
        }

        return $"{prefix}{next:D4}"; // e.g. STU20260001
    }
}