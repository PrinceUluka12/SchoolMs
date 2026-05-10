namespace SchoolMS.Core.Entities;

public class GradingScale : BaseEntity
{
    public string Name { get; set; } = null!;   // e.g. "A+"
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public string Remark { get; set; } = null!; // e.g. "Excellent"
    public decimal GradePoint { get; set; }     // e.g. 4.0
    public bool IsDefault { get; set; } = false;
}