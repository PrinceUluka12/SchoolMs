namespace SchoolMS.Core.Entities;

public class ExamSchedule : BaseEntity
{
    public Guid ExamId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid ClassId { get; set; }
    public DateTime ExamDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Venue { get; set; }
    public int? TotalSeats { get; set; }
    public Exam Exam { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public ICollection<ExamSeatingArrangement> SeatingArrangements { get; set; } = new List<ExamSeatingArrangement>();
}