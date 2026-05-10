namespace SchoolMS.Core.Entities;

public class HostelRoom : BaseEntity
{
    public string RoomNumber { get; set; } = null!;
    public int Capacity { get; set; }
    public string Type { get; set; } = "Dormitory"; // Dormitory, Single, Double
    public string Status { get; set; } = "Available"; // Available, Full, Maintenance
    public Guid BuildingId { get; set; }
    public HostelBuilding Building { get; set; } = null!;
    public ICollection<HostelAllocation> Allocations { get; set; } = new List<HostelAllocation>();
}