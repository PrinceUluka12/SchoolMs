namespace SchoolMS.Core.Entities;

public class Attendance : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid ClassId { get; set; }
    public Guid MarkedById { get; set; } // StaffId
    public DateTime Date { get; set; }
    public int? Period { get; set; }       // null = full day
    public string Status { get; set; } = null!; // Present, Absent, Late, Excused
    public string? Notes { get; set; }
    public Student Student { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public Staff MarkedBy { get; set; } = null!;
}