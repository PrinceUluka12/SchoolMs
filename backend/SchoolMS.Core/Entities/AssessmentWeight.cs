namespace SchoolMS.Core.Entities;

public class AssessmentWeight : BaseEntity
{
    public Guid ClassId { get; set; }
    public Guid TermId { get; set; }
    public string AssessmentType { get; set; } = null!;
    public decimal Weight { get; set; } // percentage e.g. 20, 30, 50
    public Class Class { get; set; } = null!;
    public Term Term { get; set; } = null!;
}