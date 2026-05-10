namespace SchoolMS.Core.Entities;

public class Grade : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TermId { get; set; }
    public Guid ClassId { get; set; }
    public Guid EnteredById { get; set; } // StaffId
    public string AssessmentType { get; set; } = null!; // CA1, CA2, MidTerm, Exam, Final
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public bool IsLocked { get; set; } = false;
    public string? TeacherRemark { get; set; }
    public Student Student { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Term Term { get; set; } = null!;
    public Class Class { get; set; } = null!;
    public Staff EnteredBy { get; set; } = null!;
}