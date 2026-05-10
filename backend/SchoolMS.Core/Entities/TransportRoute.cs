namespace SchoolMS.Core.Entities;

public class TransportRoute : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string StopsJson { get; set; } = "[]"; // JSON array of stop names
    public decimal? MorningPickupFee { get; set; }
    public decimal? AfternoonDropFee { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public ICollection<StudentTransport> StudentSubscriptions { get; set; } = new List<StudentTransport>();
}