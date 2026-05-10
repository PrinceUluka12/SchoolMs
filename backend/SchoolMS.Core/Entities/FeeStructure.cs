namespace SchoolMS.Core.Entities;

public class FeeStructure : BaseEntity
{
    public Guid FeeCategoryId { get; set; }
    public Guid TermId { get; set; }
    public Guid? ClassId { get; set; }          // null = applies to all classes
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsActive { get; set; } = true;
    public FeeCategory FeeCategory { get; set; } = null!;
    public Term Term { get; set; } = null!;
    public Class? Class { get; set; }
    public ICollection<FeeInvoice> Invoices { get; set; } = new List<FeeInvoice>();
}