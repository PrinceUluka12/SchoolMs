using SchoolMS.Core.DTOs.Finance;

namespace SchoolMS.Core.Interfaces;

public interface IFinanceService
{
    // Fee Categories
    Task<FeeCategoryResponseDto> CreateCategoryAsync(CreateFeeCategoryDto dto);
    Task<FeeCategoryResponseDto> GetCategoryByIdAsync(Guid id);
    Task<IEnumerable<FeeCategoryResponseDto>> GetCategoriesAsync();
    Task<FeeCategoryResponseDto> UpdateCategoryAsync(Guid id, UpdateFeeCategoryDto dto);
    Task DeleteCategoryAsync(Guid id);

    // Fee Structures
    Task<FeeStructureResponseDto> CreateStructureAsync(CreateFeeStructureDto dto);
    Task<FeeStructureResponseDto> GetStructureByIdAsync(Guid id);
    Task<IEnumerable<FeeStructureResponseDto>> GetStructuresAsync(Guid? termId, Guid? classId);
    Task<FeeStructureResponseDto> UpdateStructureAsync(Guid id, UpdateFeeStructureDto dto);
    Task DeleteStructureAsync(Guid id);

    // Fee Invoices
    Task<List<FeeInvoiceResponseDto>> GenerateInvoicesAsync(GenerateInvoicesDto dto);
    Task<FeeInvoiceResponseDto> GetInvoiceByIdAsync(Guid id);
    Task<StudentFeeStatementDto> GetStudentStatementAsync(Guid studentId, Guid? termId);
    Task<IEnumerable<FeeInvoiceResponseDto>> GetInvoicesByClassAsync(Guid classId, Guid termId);
    Task<FeeInvoiceResponseDto> ApplyDiscountAsync(Guid invoiceId, ApplyDiscountDto dto);
    Task WaiveInvoiceAsync(Guid invoiceId, string notes);

    // Payments
    Task<PaymentResponseDto> RecordPaymentAsync(RecordPaymentDto dto, Guid staffId);
    Task<PaymentResponseDto> GetPaymentByIdAsync(Guid id);
    Task<byte[]> GenerateReceiptPdfAsync(Guid paymentId);
    Task<IEnumerable<PaymentResponseDto>> GetPaymentsByInvoiceAsync(Guid invoiceId);

    // Reports
    Task<FinanceSummaryDto> GetFinanceSummaryAsync(Guid termId);
    Task<IEnumerable<FeeInvoiceResponseDto>> GetOverdueInvoicesAsync(Guid termId);
}