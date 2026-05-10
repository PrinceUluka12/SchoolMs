
namespace SchoolMS.Core.Entities;

public class Guardian : BaseEntity
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public string? AlternatePhone { get; set; }
    public string Relationship { get; set; } = null!; // Father, Mother, Uncle, etc.
    public string? Address { get; set; }
    public string? Occupation { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<Student> Students { get; set; } = new List<Student>();
}