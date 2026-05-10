
namespace SchoolMS.Core.Entities;



public class AcademicYear : BaseEntity
{
    public string Name { get; set; } = null!; // e.g. "2025/2026"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; } = false;
    public string Status { get; set; } = "Upcoming"; // Upcoming, Active, Closed
    public ICollection<Term> Terms { get; set; } = new List<Term>();
    public ICollection<Class> Classes { get; set; } = new List<Class>();
}