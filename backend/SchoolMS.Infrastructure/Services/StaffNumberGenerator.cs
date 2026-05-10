using Microsoft.EntityFrameworkCore;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class StaffNumberGenerator
{
    private readonly AppDbContext _db;

    public StaffNumberGenerator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateAsync()
    {
        var year = DateTime.UtcNow.Year.ToString();
        var prefix = $"STF{year}";
        var last = await _db.Staff
            .Where(s => s.StaffNumber.StartsWith(prefix))
            .OrderByDescending(s => s.StaffNumber)
            .Select(s => s.StaffNumber)
            .FirstOrDefaultAsync();

        int next = 1;
        if (last != null)
        {
            var seq = last.Substring(prefix.Length);
            if (int.TryParse(seq, out var parsed))
                next = parsed + 1;
        }

        return $"{prefix}{next:D4}"; // e.g. STF20260001
    }
}