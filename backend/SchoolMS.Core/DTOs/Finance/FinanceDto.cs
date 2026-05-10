namespace SchoolMS.Core.DTOs.Finance;

// ── Fee Category ──────────────────────────────────────────────────────────────
public class CreateFeeCategoryDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class UpdateFeeCategoryDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class FeeCategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int StructureCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Fee Structure ─────────────────────────────────────────────────────────────
public class CreateFeeStructureDto
{
    public Guid FeeCategoryId { get; set; }
    public Guid TermId { get; set; }
    public Guid? ClassId { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
}

public class UpdateFeeStructureDto
{
    public decimal? Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public bool? IsActive { get; set; }
}

public class FeeStructureResponseDto
{
    public Guid Id { get; set; }
    public Guid FeeCategoryId { get; set; }
    public string FeeCategoryName { get; set; } = null!;
    public Guid TermId { get; set; }
    public string TermName { get; set; } = null!;
    public Guid? ClassId { get; set; }
    public string? ClassName { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsActive { get; set; }
    public int InvoiceCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Fee Invoice ───────────────────────────────────────────────────────────────
public class GenerateInvoicesDto
{
    public Guid TermId { get; set; }
    public Guid? ClassId { get; set; }  // null = all classes
}

public class ApplyDiscountDto
{
    public decimal DiscountAmount { get; set; }
    public string? Notes { get; set; }
}

public class FeeInvoiceResponseDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string? ClassName { get; set; }
    public string FeeCategoryName { get; set; } = null!;
    public string TermName { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = null!;
    public DateTime DueDate { get; set; }
    public bool IsOverdue => Balance > 0 && DueDate < DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<PaymentResponseDto> Payments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// ── Payment ───────────────────────────────────────────────────────────────────
public class RecordPaymentDto
{
    public Guid FeeInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMode { get; set; } = null!;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

public class PaymentResponseDto
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = null!;
    public Guid FeeInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMode { get; set; } = null!;
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string RecordedByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// ── Finance Reports ───────────────────────────────────────────────────────────
public class FinanceSummaryDto
{
    public string TermName { get; set; } = null!;
    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal CollectionRate { get; set; }
    public int TotalInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int PartialInvoices { get; set; }
    public int UnpaidInvoices { get; set; }
    public int OverdueInvoices { get; set; }
    public List<CategorySummaryDto> ByCategory { get; set; } = new();
}

public class CategorySummaryDto
{
    public string CategoryName { get; set; } = null!;
    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal Outstanding { get; set; }
}

public class StudentFeeStatementDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string? ClassName { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalBalance { get; set; }
    public List<FeeInvoiceResponseDto> Invoices { get; set; } = new();
}