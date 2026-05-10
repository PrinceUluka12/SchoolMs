namespace SchoolMS.Core.DTOs.Timetable;

// ── Requests ──────────────────────────────────────────────────────────────────
public class CreateTimetableSlotDto
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }
    public string DayOfWeek { get; set; } = null!;
    public int Period { get; set; }
    public string StartTime { get; set; } = null!; // "HH:mm"
    public string EndTime { get; set; } = null!;
    public string? RoomNumber { get; set; }
    public Guid AcademicYearId { get; set; }
}

public class UpdateTimetableSlotDto
{
    public Guid? SubjectId { get; set; }
    public Guid? TeacherId { get; set; }
    public string? RoomNumber { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
}

// ── Responses ─────────────────────────────────────────────────────────────────
public class TimetableSlotResponseDto
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = null!;
    public string DayOfWeek { get; set; } = null!;
    public int Period { get; set; }
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
    public string? RoomNumber { get; set; }
    public Guid AcademicYearId { get; set; }
}

public class ClassTimetableDto
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public string AcademicYearName { get; set; } = null!;
    // Key = DayOfWeek, Value = ordered list of slots
    public Dictionary<string, List<TimetableSlotResponseDto>> Schedule { get; set; } = new();
}

public class TeacherTimetableDto
{
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = null!;
    public Dictionary<string, List<TimetableSlotResponseDto>> Schedule { get; set; } = new();
}

public class ConflictCheckDto
{
    public bool HasConflict { get; set; }
    public string? ConflictReason { get; set; }
}