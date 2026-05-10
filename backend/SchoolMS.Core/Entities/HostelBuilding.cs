namespace SchoolMS.Core.Entities;

public class HostelBuilding : BaseEntity
{
    public string Name { get; set; } = null!;
    public string Gender { get; set; } = null!; // Male, Female, Mixed
    public string? Description { get; set; }
    public Guid? WardenId { get; set; }          // StaffId
    public Staff? Warden { get; set; }
    public ICollection<HostelRoom> Rooms { get; set; } = new List<HostelRoom>();
}