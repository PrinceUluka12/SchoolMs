namespace SchoolMS.Core.Entities;

public class HostelAllocation : BaseEntity
{
    public Guid StudentId { get; set; }
    public Guid RoomId { get; set; }
    public Guid TermId { get; set; }
    public string BedNumber { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public Student Student { get; set; } = null!;
    public HostelRoom Room { get; set; } = null!;
    public Term Term { get; set; } = null!;
}