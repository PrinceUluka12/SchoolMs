namespace SchoolMS.Core.Entities;

public class Payment : BaseEntity
{
    public string ReceiptNumber { get; set; } = null!;
    public Guid FeeInvoiceId { get; set; }
    public Guid RecordedById { get; set; }   // StaffId
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMode { get; set; } = null!; // Cash, BankTransfer, Card, Online
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public FeeInvoice FeeInvoice { get; set; } = null!;
    public Staff RecordedBy { get; set; } = null!;
}