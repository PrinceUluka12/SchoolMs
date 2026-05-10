namespace SchoolMS.Core.Entities;


public class Term : BaseEntity
{
    public string Name { get; set; } = null!; // e.g. "First Term"
    public int TermNumber { get; set; } // 1, 2, 3
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; } = false;
    public string Status { get; set; } = "Upcoming"; // Upcoming, Active, Closed
    public Guid AcademicYearId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
}