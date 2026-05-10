namespace SchoolMS.Core.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? HeadOfDepartmentId { get; set; }
    public Staff? HeadOfDepartment { get; set; }
    public ICollection<Staff> Staff { get; set; } = new List<Staff>();
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}