namespace SchoolMS.Core.Entities;

public class Vehicle : BaseEntity
{
    public string PlateNumber { get; set; } = null!;
    public string Type { get; set; } = null!;   // Bus, Van, Mini-Bus
    public int Capacity { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public Guid? DriverId { get; set; }          // StaffId
    public string Status { get; set; } = "Active"; // Active, Maintenance, Inactive
    public Staff? Driver { get; set; }
    public ICollection<TransportRoute> Routes { get; set; } = new List<TransportRoute>();
}