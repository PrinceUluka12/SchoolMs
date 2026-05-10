namespace SchoolMS.Core.Entities;

public class Message : BaseEntity
{
    public Guid SenderId { get; set; }         // UserId
    public Guid? RecipientId { get; set; }     // null = announcement
    public string? ClassId { get; set; }        // for class-level broadcast
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Type { get; set; } = "Direct"; // Direct, ClassBroadcast, Announcement
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public bool IsDeletedBySender { get; set; } = false;
    public bool IsDeletedByRecipient { get; set; } = false;
    public User Sender { get; set; } = null!;
    public User? Recipient { get; set; }
}