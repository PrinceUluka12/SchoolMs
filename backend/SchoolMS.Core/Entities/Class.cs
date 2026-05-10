namespace SchoolMS.Core.Entities;

public class Class : BaseEntity
{
    public string Name { get; set; } = null!; // e.g. "Grade 10"
    public string? Section { get; set; } // e.g. "A", "B"
    public string? Stream { get; set; } // e.g. "Science", "Arts"
    public int Level { get; set; } // 1–12
    public int Capacity { get; set; } = 40;
    public Guid AcademicYearId { get; set; }
    public Guid? ClassTeacherId { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public Staff? ClassTeacher { get; set; }
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<ClassSubject> ClassSubjects { get; set; } = new List<ClassSubject>();
}