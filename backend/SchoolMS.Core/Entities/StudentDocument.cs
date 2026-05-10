namespace SchoolMS.Core.Entities;

public class StudentDocument : BaseEntity
{
    public string Name { get; set; } = null!; // e.g. "Birth Certificate"
    public string FileUrl { get; set; } = null!;
    public string FileType { get; set; } = null!; // pdf, jpg, etc.
    public long FileSizeBytes { get; set; }
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
}