namespace SchoolMS.Core.Entities;

public class Staff : BaseEntity
{
    public string StaffNumber { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }
    public string? PhotoUrl { get; set; }
    public string Role { get; set; } = null!; // Teacher, Finance, Librarian, Transport, Warden, Admin
    public string ContractType { get; set; } = null!; // FullTime, PartTime, Contract
    public DateTime JoinDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Inactive, Terminated
    public string? Qualifications { get; set; } // JSON array stored as string
    public string? Address { get; set; }
    public Guid UserId { get; set; }
    public Guid? DepartmentId { get; set; }
    public User User { get; set; } = null!;
    public Department? Department { get; set; }
    public ICollection<StaffDocument> Documents { get; set; } = new List<StaffDocument>();
    public ICollection<Class> ClassesAsTeacher { get; set; } = new List<Class>();
}