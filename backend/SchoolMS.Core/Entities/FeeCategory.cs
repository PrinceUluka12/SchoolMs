namespace SchoolMS.Core.Entities;

public class FeeCategory : BaseEntity
{
    public string Name { get; set; } = null!;       // Tuition, Transport, Hostel, etc.
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<FeeStructure> FeeStructures { get; set; } = new List<FeeStructure>();
}