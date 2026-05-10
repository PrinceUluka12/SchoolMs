namespace SchoolMS.Core.Entities;

public class Subject : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!; // e.g. "MTH101"
    public string Type { get; set; } = "Core"; // Core, Elective
    public int CreditHours { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
}