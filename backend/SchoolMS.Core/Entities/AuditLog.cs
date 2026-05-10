namespace SchoolMS.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorId { get; set; } // UserId who performed the action
    public string ActorEmail { get; set; } = "system";
    public string Action { get; set; } = null!; // Created, Updated, Deleted
    public string EntityType { get; set; } = null!; // e.g. "Student"
    public string EntityId { get; set; } = null!;
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}