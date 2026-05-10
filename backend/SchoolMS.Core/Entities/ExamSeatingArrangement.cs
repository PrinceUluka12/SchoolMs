namespace SchoolMS.Core.Entities;

public class ExamSeatingArrangement : BaseEntity
{
    public Guid ExamScheduleId { get; set; }
    public Guid StudentId { get; set; }
    public string SeatNumber { get; set; } = null!;
    public ExamSchedule ExamSchedule { get; set; } = null!;
    public Student Student { get; set; } = null!;
}