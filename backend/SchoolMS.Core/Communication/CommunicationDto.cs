namespace SchoolMS.Core.DTOs.Communication;

// ── Message ───────────────────────────────────────────────────────────────────
public class SendMessageDto
{
    public Guid RecipientId { get; set; }
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}

public class SendClassBroadcastDto
{
    public Guid ClassId { get; set; }
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}

public class MessageResponseDto
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = null!;
    public string SenderRole { get; set; } = null!;
    public Guid? RecipientId { get; set; }
    public string? RecipientName { get; set; }
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InboxResponseDto
{
    public int UnreadCount { get; set; }
    public List<MessageResponseDto> Messages { get; set; } = new();
}

// ── Announcement ──────────────────────────────────────────────────────────────
public class CreateAnnouncementDto
{
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Audience { get; set; } = "All";
    public bool IsPinned { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
}

public class UpdateAnnouncementDto
{
    public string? Title { get; set; }
    public string? Body { get; set; }
    public string? Audience { get; set; }
    public bool? IsPinned { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AnnouncementResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public bool IsPinned { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTime.UtcNow;
    public Guid CreatedById { get; set; }
    public string CreatedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}