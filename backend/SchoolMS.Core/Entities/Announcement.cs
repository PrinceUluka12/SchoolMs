namespace SchoolMS.Core.Entities;

public class Announcement : BaseEntity
{
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Audience { get; set; } = "All"; // All, Teachers, Students, Parents, Finance
    public bool IsPinned { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
}