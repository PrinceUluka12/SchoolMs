namespace SchoolMS.Core.DTOs.Attendance;

// ── Requests ──────────────────────────────────────────────────────────────────
public class MarkAttendanceDto
{
    public Guid ClassId { get; set; }
    public DateTime Date { get; set; }
    public int? Period { get; set; }
    public List<StudentAttendanceEntryDto> Entries { get; set; } = new();
}

public class StudentAttendanceEntryDto
{
    public Guid StudentId { get; set; }
    public string Status { get; set; } = null!; // Present, Absent, Late, Excused
    public string? Notes { get; set; }
}

public class UpdateAttendanceDto
{
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
}

// ── Responses ─────────────────────────────────────────────────────────────────
public class AttendanceResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public DateTime Date { get; set; }
    public int? Period { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public string MarkedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class AttendanceRegisterDto
{
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public DateTime Date { get; set; }
    public int? Period { get; set; }
    public bool IsMarked { get; set; }
    public List<AttendanceStudentRowDto> Students { get; set; } = new();
}

public class AttendanceStudentRowDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string? PhotoUrl { get; set; }
    public string? Status { get; set; }   // null = not yet marked
    public string? Notes { get; set; }
    public Guid? AttendanceId { get; set; }
}

public class AttendanceSummaryDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public int TotalDays { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Excused { get; set; }
    public decimal AttendanceRate => TotalDays > 0
        ? Math.Round((decimal)(Present + Late + Excused) / TotalDays * 100, 1)
        : 0;
}

public class ClassAttendanceReportDto
{
    public string ClassName { get; set; } = null!;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalStudents { get; set; }
    public decimal AverageAttendanceRate { get; set; }
    public List<AttendanceSummaryDto> Students { get; set; } = new();
}