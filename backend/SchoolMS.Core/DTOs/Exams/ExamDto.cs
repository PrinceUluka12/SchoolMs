namespace SchoolMS.Core.DTOs.Exams;

// ── Exam ──────────────────────────────────────────────────────────────────────
public class CreateExamDto
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public Guid TermId { get; set; }
    public Guid AcademicYearId { get; set; }
    public string? Description { get; set; }
}

public class UpdateExamDto
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
}

public class ExamResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Description { get; set; }
    public Guid TermId { get; set; }
    public string TermName { get; set; } = null!;
    public Guid AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = null!;
    public int ScheduleCount { get; set; }
    public List<ExamScheduleResponseDto> Schedules { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// ── Exam Schedule ─────────────────────────────────────────────────────────────
public class CreateExamScheduleDto
{
    public Guid ExamId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid ClassId { get; set; }
    public DateTime ExamDate { get; set; }
    public string StartTime { get; set; } = null!; // "HH:mm"
    public string EndTime { get; set; } = null!;
    public string? Venue { get; set; }
    public int? TotalSeats { get; set; }
}

public class ExamScheduleResponseDto
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public string ExamName { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = null!;
    public DateTime ExamDate { get; set; }
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
    public string? Venue { get; set; }
    public int? TotalSeats { get; set; }
    public int SeatedStudents { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Seating ───────────────────────────────────────────────────────────────────
public class SeatingArrangementResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string SeatNumber { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public DateTime ExamDate { get; set; }
    public string Venue { get; set; } = null!;
}

// ── Admit Card ────────────────────────────────────────────────────────────────
public class AdmitCardDto
{
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public string ExamName { get; set; } = null!;
    public string TermName { get; set; } = null!;
    public List<AdmitCardSubjectDto> Subjects { get; set; } = new();
}

public class AdmitCardSubjectDto
{
    public string SubjectName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public DateTime ExamDate { get; set; }
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
    public string? Venue { get; set; }
    public string? SeatNumber { get; set; }
}