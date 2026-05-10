namespace SchoolMS.Core.Entities;

public class FeeInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = null!;
    public Guid StudentId { get; set; }
    public Guid FeeStructureId { get; set; }
    public Guid TermId { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public string Status { get; set; } = "Unpaid"; // Unpaid, PartiallyPaid, Paid, Waived, Overdue
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }
    public Student Student { get; set; } = null!;
    public FeeStructure FeeStructure { get; set; } = null!;
    public Term Term { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public decimal Balance => Amount - PaidAmount - DiscountAmount;
}