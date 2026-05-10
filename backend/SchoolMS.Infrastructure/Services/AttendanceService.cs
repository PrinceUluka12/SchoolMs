using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Attendance;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SchoolMS.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public AttendanceService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    // ── Mark Bulk ─────────────────────────────────────────────────────────────
    public async Task<List<AttendanceResponseDto>> MarkBulkAsync(MarkAttendanceDto dto, Guid markedById)
    {
        var classExists = await _db.Classes.AnyAsync(c => c.Id == dto.ClassId);
        if (!classExists)
            throw new KeyNotFoundException($"Class {dto.ClassId} not found.");

        var allowed = new[] { "Present", "Absent", "Late", "Excused" };
        var results = new List<AttendanceResponseDto>();

        foreach (var entry in dto.Entries)
        {
            if (!allowed.Contains(entry.Status))
                throw new ArgumentException($"Invalid status '{entry.Status}' for student {entry.StudentId}.");

            // Upsert — update if already marked
            var existing = await _db.Attendances
                .FirstOrDefaultAsync(a =>
                    a.StudentId == entry.StudentId &&
                    a.Date.Date == dto.Date.Date &&
                    a.Period == dto.Period);

            if (existing != null)
            {
                existing.Status = entry.Status;
                existing.Notes = entry.Notes;
            }
            else
            {
                var attendance = new Attendance
                {
                    StudentId = entry.StudentId,
                    ClassId = dto.ClassId,
                    MarkedById = markedById,
                    Date = dto.Date.Date,
                    Period = dto.Period,
                    Status = entry.Status,
                    Notes = entry.Notes
                };
                _db.Attendances.Add(attendance);
            }
        }

        await _db.SaveChangesAsync();

        // Return the saved records
        var saved = await _db.Attendances
            .Include(a => a.Student)
            .Include(a => a.Class)
            .Include(a => a.MarkedBy)
            .Where(a =>
                a.ClassId == dto.ClassId &&
                a.Date.Date == dto.Date.Date &&
                a.Period == dto.Period)
            .ToListAsync();

        return saved.Select(MapToDto).ToList();
    }

    // ── Update Single ─────────────────────────────────────────────────────────
    public async Task<AttendanceResponseDto> UpdateAsync(Guid attendanceId, UpdateAttendanceDto dto)
    {
        var attendance = await _db.Attendances
            .Include(a => a.Student)
            .Include(a => a.Class)
            .Include(a => a.MarkedBy)
            .FirstOrDefaultAsync(a => a.Id == attendanceId)
            ?? throw new KeyNotFoundException($"Attendance record {attendanceId} not found.");

        var allowed = new[] { "Present", "Absent", "Late", "Excused" };
        if (!allowed.Contains(dto.Status))
            throw new ArgumentException($"Invalid status '{dto.Status}'.");

        attendance.Status = dto.Status;
        attendance.Notes = dto.Notes;
        await _db.SaveChangesAsync();

        return MapToDto(attendance);
    }

    // ── Get Register (class roster + today's status) ──────────────────────────
    public async Task<AttendanceRegisterDto> GetRegisterAsync(Guid classId, DateTime date, int? period)
    {
        var cls = await _db.Classes
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c => c.Id == classId)
            ?? throw new KeyNotFoundException($"Class {classId} not found.");

        var existingAttendances = await _db.Attendances
            .Where(a => a.ClassId == classId && a.Date.Date == date.Date && a.Period == period)
            .ToListAsync();

        var isMarked = existingAttendances.Any();

        var students = cls.Students
            .Where(s => s.Status == "Active")
            .OrderBy(s => s.LastName)
            .Select(s =>
            {
                var att = existingAttendances.FirstOrDefault(a => a.StudentId == s.Id);
                return new AttendanceStudentRowDto
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}",
                    StudentNumber = s.StudentNumber,
                    PhotoUrl = s.PhotoUrl,
                    Status = att?.Status,
                    Notes = att?.Notes,
                    AttendanceId = att?.Id
                };
            }).ToList();

        return new AttendanceRegisterDto
        {
            ClassId = classId,
            ClassName = cls.Name,
            Date = date.Date,
            Period = period,
            IsMarked = isMarked,
            Students = students
        };
    }

    // ── Student Summary ───────────────────────────────────────────────────────
    public async Task<List<AttendanceSummaryDto>> GetStudentSummaryAsync(
        Guid studentId, Guid? termId, DateTime? from, DateTime? to)
    {
        var student = await _db.Students.FindAsync(studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        var query = _db.Attendances.Where(a => a.StudentId == studentId);

        if (from.HasValue) query = query.Where(a => a.Date >= from.Value.Date);
        if (to.HasValue) query = query.Where(a => a.Date <= to.Value.Date);

        if (termId.HasValue)
        {
            var term = await _db.Terms.FindAsync(termId.Value);
            if (term != null)
            {
                query = query.Where(a => a.Date >= term.StartDate && a.Date <= term.EndDate);
            }
        }

        var records = await query.ToListAsync();

        var summary = new AttendanceSummaryDto
        {
            StudentId = studentId,
            StudentName = $"{student.FirstName} {student.LastName}",
            StudentNumber = student.StudentNumber,
            TotalDays = records.Select(r => r.Date.Date).Distinct().Count(),
            Present = records.Count(r => r.Status == "Present"),
            Absent = records.Count(r => r.Status == "Absent"),
            Late = records.Count(r => r.Status == "Late"),
            Excused = records.Count(r => r.Status == "Excused"),
        };

        return new List<AttendanceSummaryDto> { summary };
    }

    // ── Class Report ──────────────────────────────────────────────────────────
    public async Task<ClassAttendanceReportDto> GetClassReportAsync(Guid classId, DateTime from, DateTime to)
    {
        var cls = await _db.Classes
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c => c.Id == classId)
            ?? throw new KeyNotFoundException($"Class {classId} not found.");

        var attendances = await _db.Attendances
            .Where(a => a.ClassId == classId && a.Date >= from.Date && a.Date <= to.Date)
            .ToListAsync();

        var summaries = cls.Students
            .Where(s => s.Status == "Active")
            .Select(s =>
            {
                var studentAtt = attendances.Where(a => a.StudentId == s.Id).ToList();
                return new AttendanceSummaryDto
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}",
                    StudentNumber = s.StudentNumber,
                    TotalDays = studentAtt.Select(a => a.Date.Date).Distinct().Count(),
                    Present = studentAtt.Count(a => a.Status == "Present"),
                    Absent = studentAtt.Count(a => a.Status == "Absent"),
                    Late = studentAtt.Count(a => a.Status == "Late"),
                    Excused = studentAtt.Count(a => a.Status == "Excused"),
                };
            }).ToList();

        var avgRate = summaries.Any()
            ? Math.Round(summaries.Average(s => s.AttendanceRate), 1)
            : 0;

        return new ClassAttendanceReportDto
        {
            ClassName = cls.Name,
            FromDate = from,
            ToDate = to,
            TotalStudents = summaries.Count,
            AverageAttendanceRate = avgRate,
            Students = summaries.OrderBy(s => s.StudentName).ToList()
        };
    }

    // ── Low Attendance Flag ───────────────────────────────────────────────────
    public async Task<List<AttendanceSummaryDto>> GetLowAttendanceAsync(
        Guid classId, Guid termId, decimal threshold)
    {
        var report = await GetClassReportAsync(classId,
            (await _db.Terms.FindAsync(termId))!.StartDate,
            (await _db.Terms.FindAsync(termId))!.EndDate);

        return report.Students
            .Where(s => s.AttendanceRate < threshold)
            .OrderBy(s => s.AttendanceRate)
            .ToList();
    }

    // ── Private Map ───────────────────────────────────────────────────────────
    private static AttendanceResponseDto MapToDto(Attendance a) => new()
    {
        Id = a.Id,
        StudentId = a.StudentId,
        StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
        StudentNumber = a.Student.StudentNumber,
        ClassId = a.ClassId,
        ClassName = a.Class.Name,
        Date = a.Date,
        Period = a.Period,
        Status = a.Status,
        Notes = a.Notes,
        MarkedByName = $"{a.MarkedBy.FirstName} {a.MarkedBy.LastName}",
        CreatedAt = a.CreatedAt
    };

    public async Task<Guid> GetStaffByUserIdAsync(Guid userId)
    {
        var staff = await _db.Staff.FirstOrDefaultAsync(s => s.UserId == userId)
            ?? throw new UnauthorizedAccessException("No staff record linked to this user.");
        return staff.Id;
    }
}