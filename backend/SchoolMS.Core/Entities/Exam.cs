namespace SchoolMS.Core.Entities;

public class Exam : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Type { get; set; } = null!; // MidTerm, EndOfTerm, Mock, Entrance
    public Guid TermId { get; set; }
    public Guid AcademicYearId { get; set; }
    public string Status { get; set; } = "Scheduled"; // Scheduled, Ongoing, Completed, Cancelled
    public string? Description { get; set; }
    public Term Term { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public ICollection<ExamSchedule> Schedules { get; set; } = new List<ExamSchedule>();
}