namespace SchoolMS.Core.Entities;

public class StudentTransport : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid RouteId { get; set; }
    public Guid TermId { get; set; }
    public string PickupStop { get; set; } = null!;
    public string DropStop { get; set; } = null!;
    public string Type { get; set; } = "Both"; // Morning, Afternoon, Both
    public bool IsActive { get; set; } = true;
    public Student Student { get; set; } = null!;
    public TransportRoute Route { get; set; } = null!;
    public Term Term { get; set; } = null!;
}