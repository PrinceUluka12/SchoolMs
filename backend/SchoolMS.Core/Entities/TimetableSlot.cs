namespace SchoolMS.Core.Entities;

public class TimetableSlot : BaseEntity
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TeacherId { get; set; }
    public string DayOfWeek { get; set; } = null!; // Monday..Friday
    public int Period { get; set; }               // 1–8
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? RoomNumber { get; set; }
    public Guid AcademicYearId { get; set; }
    public Class Class { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Staff Teacher { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
}