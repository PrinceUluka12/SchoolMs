namespace SchoolMS.Core.Entities;

public class Student : BaseEntity
{
    public string StudentNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = null!;
    public string? PhotoUrl { get; set; }
    public string? Address { get; set; }
    public string? MedicalNotes { get; set; }
    public string Status { get; set; } = "Active"; // Active, Inactive, Transferred, Graduated, Withdrawn
    public Guid? ClassId { get; set; }
    public Guid UserId { get; set; }
    public Guid GuardianId { get; set; }
    public Class? Class { get; set; }
    public User User { get; set; } = null!;
    public Guardian Guardian { get; set; } = null!;
    public ICollection<StudentDocument> Documents { get; set; } = new List<StudentDocument>();
}