namespace SchoolMS.Core.Entities;

public class ClassSubject : BaseEntity
{
    public Guid ClassId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid? TeacherId { get; set; }
    public bool IsActive { get; set; } = true;
    public Class Class { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Staff? Teacher { get; set; }
}