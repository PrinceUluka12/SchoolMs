namespace SchoolMS.Core.Entities;

public class StaffDocument : BaseEntity
{
    public string Name { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public Guid StaffId { get; set; }
    public Staff Staff { get; set; } = null!;
}