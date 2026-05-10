using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Timetable;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class TimetableService : ITimetableService
{
    private readonly AppDbContext _db;

    private static readonly string[] DaysOrdered =
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

    public TimetableService(AppDbContext db)
    {
        _db = db;
    }

    // ── Create Slot ───────────────────────────────────────────────────────────
    public async Task<TimetableSlotResponseDto> CreateSlotAsync(CreateTimetableSlotDto dto)
    {
        var conflict = await CheckConflictAsync(dto);
        if (conflict.HasConflict)
            throw new InvalidOperationException(conflict.ConflictReason);

        var slot = new TimetableSlot
        {
            ClassId = dto.ClassId,
            SubjectId = dto.SubjectId,
            TeacherId = dto.TeacherId,
            DayOfWeek = dto.DayOfWeek,
            Period = dto.Period,
            StartTime = TimeSpan.Parse(dto.StartTime),
            EndTime = TimeSpan.Parse(dto.EndTime),
            RoomNumber = dto.RoomNumber,
            AcademicYearId = dto.AcademicYearId
        };

        _db.TimetableSlots.Add(slot);
        await _db.SaveChangesAsync();
        return await MapAsync(slot.Id);
    }

    // ── Update Slot ───────────────────────────────────────────────────────────
    public async Task<TimetableSlotResponseDto> UpdateSlotAsync(Guid id, UpdateTimetableSlotDto dto)
    {
        var slot = await _db.TimetableSlots.FindAsync(id)
            ?? throw new KeyNotFoundException($"Timetable slot {id} not found.");

        if (dto.SubjectId.HasValue) slot.SubjectId = dto.SubjectId.Value;
        if (dto.TeacherId.HasValue) slot.TeacherId = dto.TeacherId.Value;
        if (dto.RoomNumber != null) slot.RoomNumber = dto.RoomNumber;
        if (dto.StartTime != null) slot.StartTime = TimeSpan.Parse(dto.StartTime);
        if (dto.EndTime != null) slot.EndTime = TimeSpan.Parse(dto.EndTime);

        await _db.SaveChangesAsync();
        return await MapAsync(id);
    }

    // ── Delete Slot ───────────────────────────────────────────────────────────
    public async Task DeleteSlotAsync(Guid id)
    {
        var slot = await _db.TimetableSlots.FindAsync(id)
            ?? throw new KeyNotFoundException($"Timetable slot {id} not found.");
        _db.TimetableSlots.Remove(slot);
        await _db.SaveChangesAsync();
    }

    // ── Class Timetable ───────────────────────────────────────────────────────
    public async Task<ClassTimetableDto> GetClassTimetableAsync(Guid classId, Guid academicYearId)
    {
        var cls = await _db.Classes
            .Include(c => c.AcademicYear)
            .FirstOrDefaultAsync(c => c.Id == classId)
            ?? throw new KeyNotFoundException($"Class {classId} not found.");

        var slots = await _db.TimetableSlots
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
            .Include(s => s.Class)
            .Where(s => s.ClassId == classId && s.AcademicYearId == academicYearId)
            .OrderBy(s => s.Period)
            .ToListAsync();

        var schedule = new Dictionary<string, List<TimetableSlotResponseDto>>();
        foreach (var day in DaysOrdered)
        {
            schedule[day] = slots
                .Where(s => s.DayOfWeek == day)
                .Select(MapSlotToDto)
                .ToList();
        }

        return new ClassTimetableDto
        {
            ClassId = classId,
            ClassName = cls.Name,
            AcademicYearName = cls.AcademicYear?.Name ?? "",
            Schedule = schedule
        };
    }

    // ── Teacher Timetable ─────────────────────────────────────────────────────
    public async Task<TeacherTimetableDto> GetTeacherTimetableAsync(Guid teacherId, Guid academicYearId)
    {
        var teacher = await _db.Staff.FindAsync(teacherId)
            ?? throw new KeyNotFoundException($"Staff {teacherId} not found.");

        var slots = await _db.TimetableSlots
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
            .Include(s => s.Class)
            .Where(s => s.TeacherId == teacherId && s.AcademicYearId == academicYearId)
            .OrderBy(s => s.Period)
            .ToListAsync();

        var schedule = new Dictionary<string, List<TimetableSlotResponseDto>>();
        foreach (var day in DaysOrdered)
        {
            schedule[day] = slots
                .Where(s => s.DayOfWeek == day)
                .Select(MapSlotToDto)
                .ToList();
        }

        return new TeacherTimetableDto
        {
            TeacherId = teacherId,
            TeacherName = $"{teacher.FirstName} {teacher.LastName}",
            Schedule = schedule
        };
    }

    // ── Conflict Check ────────────────────────────────────────────────────────
    public async Task<ConflictCheckDto> CheckConflictAsync(CreateTimetableSlotDto dto, Guid? excludeSlotId = null)
    {
        var query = _db.TimetableSlots
            .Where(s =>
                s.AcademicYearId == dto.AcademicYearId &&
                s.DayOfWeek == dto.DayOfWeek &&
                s.Period == dto.Period);

        if (excludeSlotId.HasValue)
            query = query.Where(s => s.Id != excludeSlotId.Value);

        // Teacher double-booking
        var teacherConflict = await query
            .AnyAsync(s => s.TeacherId == dto.TeacherId);
        if (teacherConflict)
            return new ConflictCheckDto
            {
                HasConflict = true,
                ConflictReason = $"Teacher is already assigned to another class on {dto.DayOfWeek} period {dto.Period}."
            };

        // Class already has a slot for this period
        var classConflict = await query
            .AnyAsync(s => s.ClassId == dto.ClassId);
        if (classConflict)
            return new ConflictCheckDto
            {
                HasConflict = true,
                ConflictReason = $"Class already has a subject assigned on {dto.DayOfWeek} period {dto.Period}."
            };

        return new ConflictCheckDto { HasConflict = false };
    }

    // ── Get All Slots ─────────────────────────────────────────────────────────
    public async Task<List<TimetableSlotResponseDto>> GetAllSlotsAsync(Guid academicYearId, Guid? classId)
    {
        var query = _db.TimetableSlots
            .Include(s => s.Class)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
            .Where(s => s.AcademicYearId == academicYearId);

        if (classId.HasValue)
            query = query.Where(s => s.ClassId == classId.Value);

        var slots = await query
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.Period)
            .ToListAsync();

        return slots.Select(MapSlotToDto).ToList();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task<TimetableSlotResponseDto> MapAsync(Guid id)
    {
        var slot = await _db.TimetableSlots
            .Include(s => s.Class)
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException();
        return MapSlotToDto(slot);
    }

    private static TimetableSlotResponseDto MapSlotToDto(TimetableSlot s) => new()
    {
        Id = s.Id,
        ClassId = s.ClassId,
        ClassName = s.Class?.Name ?? "",
        SubjectId = s.SubjectId,
        SubjectName = s.Subject?.Name ?? "",
        SubjectCode = s.Subject?.Code ?? "",
        TeacherId = s.TeacherId,
        TeacherName = s.Teacher != null ? $"{s.Teacher.FirstName} {s.Teacher.LastName}" : "",
        DayOfWeek = s.DayOfWeek,
        Period = s.Period,
        StartTime = s.StartTime.ToString(@"hh\:mm"),
        EndTime = s.EndTime.ToString(@"hh\:mm"),
        RoomNumber = s.RoomNumber,
        AcademicYearId = s.AcademicYearId
    };
}