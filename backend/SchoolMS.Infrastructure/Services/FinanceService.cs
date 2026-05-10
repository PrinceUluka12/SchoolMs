using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolMS.Core.DTOs.Finance;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class FinanceService : IFinanceService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;

    public FinanceService(AppDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    // ── Fee Categories ────────────────────────────────────────────────────────
    public async Task<FeeCategoryResponseDto> CreateCategoryAsync(CreateFeeCategoryDto dto)
    {
        var exists = await _db.FeeCategories.AnyAsync(c => c.Name == dto.Name);
        if (exists) throw new InvalidOperationException($"Category '{dto.Name}' already exists.");

        var cat = new FeeCategory
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            IsActive = true
        };
        _db.FeeCategories.Add(cat);
        await _db.SaveChangesAsync();
        return await MapCategoryAsync(cat.Id);
    }

    public async Task<FeeCategoryResponseDto> GetCategoryByIdAsync(Guid id)
    {
        var exists = await _db.FeeCategories.AnyAsync(c => c.Id == id);
        if (!exists) throw new KeyNotFoundException($"Fee category {id} not found.");
        return await MapCategoryAsync(id);
    }

    public async Task<IEnumerable<FeeCategoryResponseDto>> GetCategoriesAsync()
    {
        var cats = await _db.FeeCategories
            .Include(c => c.FeeStructures)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return cats.Select(c => new FeeCategoryResponseDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive,
            StructureCount = c.FeeStructures.Count,
            CreatedAt = c.CreatedAt
        });
    }

    public async Task<FeeCategoryResponseDto> UpdateCategoryAsync(Guid id, UpdateFeeCategoryDto dto)
    {
        var cat = await _db.FeeCategories.FindAsync(id)
            ?? throw new KeyNotFoundException($"Fee category {id} not found.");

        if (dto.Name != null) cat.Name = dto.Name.Trim();
        if (dto.Description != null) cat.Description = dto.Description;
        if (dto.IsActive.HasValue) cat.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return await MapCategoryAsync(id);
    }

    public async Task DeleteCategoryAsync(Guid id)
    {
        var cat = await _db.FeeCategories
            .Include(c => c.FeeStructures)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Fee category {id} not found.");

        if (cat.FeeStructures.Any(f => f.IsActive))
            throw new InvalidOperationException("Cannot delete a category with active fee structures.");

        cat.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Fee Structures ────────────────────────────────────────────────────────
    public async Task<FeeStructureResponseDto> CreateStructureAsync(CreateFeeStructureDto dto)
    {
        if (dto.Amount <= 0) throw new ArgumentException("Amount must be greater than zero.");

        var structure = new FeeStructure
        {
            FeeCategoryId = dto.FeeCategoryId,
            TermId = dto.TermId,
            ClassId = dto.ClassId,
            Amount = dto.Amount,
            DueDate = dto.DueDate,
            IsActive = true
        };
        _db.FeeStructures.Add(structure);
        await _db.SaveChangesAsync();
        return await MapStructureAsync(structure.Id);
    }

    public async Task<FeeStructureResponseDto> GetStructureByIdAsync(Guid id)
    {
        var exists = await _db.FeeStructures.AnyAsync(s => s.Id == id);
        if (!exists) throw new KeyNotFoundException($"Fee structure {id} not found.");
        return await MapStructureAsync(id);
    }

    public async Task<IEnumerable<FeeStructureResponseDto>> GetStructuresAsync(Guid? termId, Guid? classId)
    {
        var query = _db.FeeStructures
            .Include(s => s.FeeCategory)
            .Include(s => s.Term)
            .Include(s => s.Class)
            .Include(s => s.Invoices)
            .AsQueryable();

        if (termId.HasValue) query = query.Where(s => s.TermId == termId);
        if (classId.HasValue) query = query.Where(s => s.ClassId == classId || s.ClassId == null);

        var structures = await query.OrderBy(s => s.FeeCategory.Name).ToListAsync();

        return structures.Select(s => new FeeStructureResponseDto
        {
            Id = s.Id,
            FeeCategoryId = s.FeeCategoryId,
            FeeCategoryName = s.FeeCategory.Name,
            TermId = s.TermId,
            TermName = s.Term.Name,
            ClassId = s.ClassId,
            ClassName = s.Class?.Name,
            Amount = s.Amount,
            DueDate = s.DueDate,
            IsActive = s.IsActive,
            InvoiceCount = s.Invoices.Count,
            CreatedAt = s.CreatedAt
        });
    }

    public async Task<FeeStructureResponseDto> UpdateStructureAsync(Guid id, UpdateFeeStructureDto dto)
    {
        var structure = await _db.FeeStructures.FindAsync(id)
            ?? throw new KeyNotFoundException($"Fee structure {id} not found.");

        if (dto.Amount.HasValue) structure.Amount = dto.Amount.Value;
        if (dto.DueDate.HasValue) structure.DueDate = dto.DueDate.Value;
        if (dto.IsActive.HasValue) structure.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return await MapStructureAsync(id);
    }

    public async Task DeleteStructureAsync(Guid id)
    {
        var structure = await _db.FeeStructures
            .Include(s => s.Invoices)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Fee structure {id} not found.");

        if (structure.Invoices.Any())
            throw new InvalidOperationException("Cannot delete a fee structure with existing invoices.");

        structure.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Fee Invoices ──────────────────────────────────────────────────────────
    public async Task<List<FeeInvoiceResponseDto>> GenerateInvoicesAsync(GenerateInvoicesDto dto)
    {
        // Get active fee structures for this term
        var structures = await _db.FeeStructures
            .Where(s => s.TermId == dto.TermId && s.IsActive)
            .ToListAsync();

        if (!structures.Any())
            throw new InvalidOperationException("No active fee structures found for this term.");

        // Get students
        var studentQuery = _db.Students.Where(s => s.Status == "Active");
        if (dto.ClassId.HasValue)
            studentQuery = studentQuery.Where(s => s.ClassId == dto.ClassId);

        var students = await studentQuery.ToListAsync();
        var generated = new List<FeeInvoiceResponseDto>();

        foreach (var student in students)
        {
            // Apply structures that match this student's class or are school-wide
            var applicableStructures = structures
                .Where(s => s.ClassId == null || s.ClassId == student.ClassId)
                .ToList();

            foreach (var structure in applicableStructures)
            {
                // Skip if invoice already exists
                var exists = await _db.FeeInvoices.AnyAsync(i =>
                    i.StudentId == student.Id &&
                    i.FeeStructureId == structure.Id &&
                    i.TermId == dto.TermId);

                if (exists) continue;

                var invoiceNumber = await GenerateInvoiceNumberAsync();

                var invoice = new FeeInvoice
                {
                    InvoiceNumber = invoiceNumber,
                    StudentId = student.Id,
                    FeeStructureId = structure.Id,
                    TermId = dto.TermId,
                    Amount = structure.Amount,
                    PaidAmount = 0,
                    DiscountAmount = 0,
                    DueDate = structure.DueDate,
                    Status = "Unpaid"
                };

                _db.FeeInvoices.Add(invoice);
                await _db.SaveChangesAsync();

                generated.Add(await MapInvoiceAsync(invoice.Id));
            }
        }

        return generated;
    }

    public async Task<FeeInvoiceResponseDto> GetInvoiceByIdAsync(Guid id)
    {
        var exists = await _db.FeeInvoices.AnyAsync(i => i.Id == id);
        if (!exists) throw new KeyNotFoundException($"Invoice {id} not found.");
        return await MapInvoiceAsync(id);
    }

    public async Task<StudentFeeStatementDto> GetStudentStatementAsync(Guid studentId, Guid? termId)
    {
        var student = await _db.Students
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.Id == studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        var query = _db.FeeInvoices
            .Include(i => i.FeeStructure).ThenInclude(f => f.FeeCategory)
            .Include(i => i.Term)
            .Include(i => i.Payments).ThenInclude(p => p.RecordedBy)
            .Where(i => i.StudentId == studentId);

        if (termId.HasValue)
            query = query.Where(i => i.TermId == termId);

        var invoices = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();
        var invoiceDtos = invoices.Select(MapInvoiceToDto).ToList();

        return new StudentFeeStatementDto
        {
            StudentId = studentId,
            StudentName = $"{student.FirstName} {student.LastName}",
            StudentNumber = student.StudentNumber,
            ClassName = student.Class?.Name,
            TotalBilled = invoiceDtos.Sum(i => i.Amount),
            TotalPaid = invoiceDtos.Sum(i => i.PaidAmount),
            TotalBalance = invoiceDtos.Sum(i => i.Balance),
            Invoices = invoiceDtos
        };
    }

    public async Task<IEnumerable<FeeInvoiceResponseDto>> GetInvoicesByClassAsync(Guid classId, Guid termId)
    {
        var invoices = await _db.FeeInvoices
            .Include(i => i.Student).ThenInclude(s => s.Class)
            .Include(i => i.FeeStructure).ThenInclude(f => f.FeeCategory)
            .Include(i => i.Term)
            .Include(i => i.Payments).ThenInclude(p => p.RecordedBy)
            .Where(i => i.TermId == termId && i.Student.ClassId == classId)
            .OrderBy(i => i.Student.LastName)
            .ToListAsync();

        return invoices.Select(MapInvoiceToDto);
    }

    public async Task<FeeInvoiceResponseDto> ApplyDiscountAsync(Guid invoiceId, ApplyDiscountDto dto)
    {
        var invoice = await _db.FeeInvoices.FindAsync(invoiceId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        if (dto.DiscountAmount > invoice.Amount - invoice.PaidAmount)
            throw new ArgumentException("Discount cannot exceed outstanding balance.");

        invoice.DiscountAmount += dto.DiscountAmount;
        invoice.Notes = dto.Notes;
        UpdateInvoiceStatus(invoice);
        await _db.SaveChangesAsync();
        return await MapInvoiceAsync(invoiceId);
    }

    public async Task WaiveInvoiceAsync(Guid invoiceId, string notes)
    {
        var invoice = await _db.FeeInvoices.FindAsync(invoiceId)
            ?? throw new KeyNotFoundException($"Invoice {invoiceId} not found.");

        invoice.Status = "Waived";
        invoice.Notes = notes;
        await _db.SaveChangesAsync();
    }

    // ── Payments ──────────────────────────────────────────────────────────────
    public async Task<PaymentResponseDto> RecordPaymentAsync(RecordPaymentDto dto, Guid staffId)
    {
        var invoice = await _db.FeeInvoices
            .Include(i => i.Student).ThenInclude(s => s.Guardian)
            .Include(i => i.FeeStructure).ThenInclude(f => f.FeeCategory)
            .FirstOrDefaultAsync(i => i.Id == dto.FeeInvoiceId)
            ?? throw new KeyNotFoundException($"Invoice {dto.FeeInvoiceId} not found.");

        if (invoice.Status == "Paid")
            throw new InvalidOperationException("This invoice is already fully paid.");

        if (invoice.Status == "Waived")
            throw new InvalidOperationException("Cannot record payment for a waived invoice.");

        if (dto.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.");

        if (dto.Amount > invoice.Balance)
            throw new ArgumentException($"Payment amount ({dto.Amount:C}) exceeds balance ({invoice.Balance:C}).");

        var receiptNumber = await GenerateReceiptNumberAsync();

        var payment = new Payment
        {
            ReceiptNumber = receiptNumber,
            FeeInvoiceId = dto.FeeInvoiceId,
            RecordedById = staffId,
            Amount = dto.Amount,
            PaymentDate = dto.PaymentDate,
            PaymentMode = dto.PaymentMode,
            ReferenceNumber = dto.ReferenceNumber,
            Notes = dto.Notes
        };

        _db.Payments.Add(payment);

        // Update invoice
        invoice.PaidAmount += dto.Amount;
        UpdateInvoiceStatus(invoice);

        await _db.SaveChangesAsync();

        // Send receipt email (fire and forget)
        if (invoice.Student.Guardian != null)
        {
            _ = _email.SendReceiptAsync(
                invoice.Student.Guardian.Email,
                $"{invoice.Student.Guardian.FirstName} {invoice.Student.Guardian.LastName}",
                $"{invoice.Student.FirstName} {invoice.Student.LastName}",
                receiptNumber,
                dto.Amount);
        }

        return await MapPaymentAsync(payment.Id);
    }

    public async Task<PaymentResponseDto> GetPaymentByIdAsync(Guid id)
    {
        var exists = await _db.Payments.AnyAsync(p => p.Id == id);
        if (!exists) throw new KeyNotFoundException($"Payment {id} not found.");
        return await MapPaymentAsync(id);
    }

    public async Task<IEnumerable<PaymentResponseDto>> GetPaymentsByInvoiceAsync(Guid invoiceId)
    {
        var payments = await _db.Payments
            .Include(p => p.FeeInvoice).ThenInclude(i => i.Student)
            .Include(p => p.RecordedBy)
            .Where(p => p.FeeInvoiceId == invoiceId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync();

        return payments.Select(MapPaymentToDto);
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(Guid paymentId)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var payment = await _db.Payments
            .Include(p => p.RecordedBy)
            .Include(p => p.FeeInvoice).ThenInclude(i => i.Student).ThenInclude(s => s.Class)
            .Include(p => p.FeeInvoice).ThenInclude(i => i.FeeStructure).ThenInclude(f => f.FeeCategory)
            .Include(p => p.FeeInvoice).ThenInclude(i => i.Term)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new KeyNotFoundException($"Payment {paymentId} not found.");

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("SCHOOL MANAGEMENT SYSTEM")
                                .Bold().FontSize(16).FontColor("#1F3864");
                            c.Item().Text("Official Fee Receipt")
                                .FontSize(11).FontColor("#2E75B6");
                        });
                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().Background("#1F3864").Padding(8).Column(inner =>
                            {
                                inner.Item().Text("RECEIPT").Bold().FontColor("#FFFFFF").AlignCenter();
                                inner.Item().Text(payment.ReceiptNumber)
                                    .Bold().FontSize(12).FontColor("#FFFFFF").AlignCenter();
                            });
                        });
                    });

                    col.Item().LineHorizontal(2).LineColor("#2E75B6");
                    col.Item().Height(12);

                    // Details grid
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });

                        void InfoCell(string label, string value, string? color = null)
                        {
                            table.Cell().Padding(6).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8).FontColor("#888888");
                                c.Item().Text(value).Bold()
                                    .FontColor(color ?? "#000000");
                            });
                        }

                        var student = payment.FeeInvoice.Student;
                        InfoCell("Student Name", $"{student.FirstName} {student.LastName}");
                        InfoCell("Student Number", student.StudentNumber);
                        InfoCell("Class", student.Class?.Name ?? "—");
                        InfoCell("Term", payment.FeeInvoice.Term.Name);
                        InfoCell("Fee Category", payment.FeeInvoice.FeeStructure.FeeCategory.Name);
                        InfoCell("Invoice No.", payment.FeeInvoice.InvoiceNumber);
                        InfoCell("Payment Date", payment.PaymentDate.ToString("dd MMM yyyy"));
                        InfoCell("Payment Mode", payment.PaymentMode);
                    });

                    col.Item().Height(12);

                    // Amount box
                    col.Item().Background("#D1FAE5").Border(1).BorderColor("#10B981")
                        .Padding(12).Row(row =>
                        {
                            row.RelativeItem().Text("AMOUNT PAID").Bold().FontSize(11).FontColor("#059669");
                            row.RelativeItem().AlignRight()
                                .Text(payment.Amount.ToString("C")).Bold().FontSize(18).FontColor("#059669");
                        });

                    col.Item().Height(12);

                    // Reference & recorded by
                    if (!string.IsNullOrEmpty(payment.ReferenceNumber))
                    {
                        col.Item().Text($"Reference: {payment.ReferenceNumber}")
                            .FontSize(9).FontColor("#666666");
                    }

                    col.Item().Text(
                        $"Recorded by: {payment.RecordedBy.FirstName} {payment.RecordedBy.LastName}")
                        .FontSize(9).FontColor("#666666");

                    col.Item().Height(20);

                    // Signature lines
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                            c.Item().Text("Finance Officer Signature").FontSize(8).FontColor("#999999");
                        });
                        row.ConstantItem(60);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                            c.Item().Text("Parent / Guardian Signature").FontSize(8).FontColor("#999999");
                        });
                    });

                    col.Item().Height(10);
                    col.Item().AlignCenter().Text(
                        $"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC")
                        .FontSize(8).FontColor("#CCCCCC");
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ── Reports ───────────────────────────────────────────────────────────────
    public async Task<FinanceSummaryDto> GetFinanceSummaryAsync(Guid termId)
    {
        var term = await _db.Terms.FindAsync(termId)
            ?? throw new KeyNotFoundException($"Term {termId} not found.");

        var invoices = await _db.FeeInvoices
            .Include(i => i.FeeStructure).ThenInclude(f => f.FeeCategory)
            .Where(i => i.TermId == termId)
            .ToListAsync();

        var byCategory = invoices
            .GroupBy(i => i.FeeStructure.FeeCategory.Name)
            .Select(g => new CategorySummaryDto
            {
                CategoryName = g.Key,
                TotalBilled = g.Sum(i => i.Amount),
                TotalCollected = g.Sum(i => i.PaidAmount),
                Outstanding = g.Sum(i => i.Balance)
            }).ToList();

        var totalBilled = invoices.Sum(i => i.Amount);
        var totalCollected = invoices.Sum(i => i.PaidAmount);

        return new FinanceSummaryDto
        {
            TermName = term.Name,
            TotalBilled = totalBilled,
            TotalCollected = totalCollected,
            TotalOutstanding = invoices.Sum(i => i.Balance),
            TotalDiscounts = invoices.Sum(i => i.DiscountAmount),
            CollectionRate = totalBilled > 0
                ? Math.Round(totalCollected / totalBilled * 100, 1) : 0,
            TotalInvoices = invoices.Count,
            PaidInvoices = invoices.Count(i => i.Status == "Paid"),
            PartialInvoices = invoices.Count(i => i.Status == "PartiallyPaid"),
            UnpaidInvoices = invoices.Count(i => i.Status == "Unpaid"),
            OverdueInvoices = invoices.Count(i =>
                i.Balance > 0 && i.DueDate < DateTime.UtcNow && i.Status != "Waived"),
            ByCategory = byCategory
        };
    }

    public async Task<IEnumerable<FeeInvoiceResponseDto>> GetOverdueInvoicesAsync(Guid termId)
    {
        var invoices = await _db.FeeInvoices
            .Include(i => i.Student).ThenInclude(s => s.Class)
            .Include(i => i.FeeStructure).ThenInclude(f => f.FeeCategory)
            .Include(i => i.Term)
            .Include(i => i.Payments).ThenInclude(p => p.RecordedBy)
            .Where(i => i.TermId == termId &&
                        i.Balance > 0 &&
                        i.DueDate < DateTime.UtcNow &&
                        i.Status != "Waived")
            .OrderBy(i => i.DueDate)
            .ToListAsync();

        return invoices.Select(MapInvoiceToDto);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private static void UpdateInvoiceStatus(FeeInvoice invoice)
    {
        if (invoice.Balance <= 0)
            invoice.Status = "Paid";
        else if (invoice.PaidAmount > 0)
            invoice.Status = "PartiallyPaid";
        else if (invoice.DueDate < DateTime.UtcNow)
            invoice.Status = "Overdue";
        else
            invoice.Status = "Unpaid";
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV{year}";
        var last = await _db.FeeInvoices
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync();

        int next = 1;
        if (last != null && int.TryParse(last[prefix.Length..], out var parsed))
            next = parsed + 1;

        return $"{prefix}{next:D5}";
    }

    private async Task<string> GenerateReceiptNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RCP{year}";
        var last = await _db.Payments
            .Where(p => p.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(p => p.ReceiptNumber)
            .Select(p => p.ReceiptNumber)
            .FirstOrDefaultAsync();

        int next = 1;
        if (last != null && int.TryParse(last[prefix.Length..], out var parsed))
            next = parsed + 1;

        return $"{prefix}{next:D5}";
    }

    private async Task<FeeCategoryResponseDto> MapCategoryAsync(Guid id)
    {
        var c = await _db.FeeCategories
            .Include(x => x.FeeStructures)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();

        return new FeeCategoryResponseDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive,
            StructureCount = c.FeeStructures.Count,
            CreatedAt = c.CreatedAt
        };
    }

    private async Task<FeeStructureResponseDto> MapStructureAsync(Guid id)
    {
        var s = await _db.FeeStructures
            .Include(x => x.FeeCategory)
            .Include(x => x.Term)
            .Include(x => x.Class)
            .Include(x => x.Invoices)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();

        return new FeeStructureResponseDto
        {
            Id = s.Id,
            FeeCategoryId = s.FeeCategoryId,
            FeeCategoryName = s.FeeCategory.Name,
            TermId = s.TermId,
            TermName = s.Term.Name,
            ClassId = s.ClassId,
            ClassName = s.Class?.Name,
            Amount = s.Amount,
            DueDate = s.DueDate,
            IsActive = s.IsActive,
            InvoiceCount = s.Invoices.Count,
            CreatedAt = s.CreatedAt
        };
    }

    private async Task<FeeInvoiceResponseDto> MapInvoiceAsync(Guid id)
    {
        var i = await _db.FeeInvoices
            .Include(x => x.Student).ThenInclude(s => s.Class)
            .Include(x => x.FeeStructure).ThenInclude(f => f.FeeCategory)
            .Include(x => x.Term)
            .Include(x => x.Payments).ThenInclude(p => p.RecordedBy)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();

        return MapInvoiceToDto(i);
    }

    private static FeeInvoiceResponseDto MapInvoiceToDto(FeeInvoice i) => new()
    {
        Id = i.Id,
        InvoiceNumber = i.InvoiceNumber,
        StudentId = i.StudentId,
        StudentName = $"{i.Student.FirstName} {i.Student.LastName}",
        StudentNumber = i.Student.StudentNumber,
        ClassName = i.Student.Class?.Name,
        FeeCategoryName = i.FeeStructure.FeeCategory.Name,
        TermName = i.Term.Name,
        Amount = i.Amount,
        PaidAmount = i.PaidAmount,
        DiscountAmount = i.DiscountAmount,
        Balance = i.Balance,
        Status = i.Status,
        DueDate = i.DueDate,
        Notes = i.Notes,
        Payments = i.Payments.Select(MapPaymentToDto).ToList(),
        CreatedAt = i.CreatedAt
    };

    private async Task<PaymentResponseDto> MapPaymentAsync(Guid id)
    {
        var p = await _db.Payments
            .Include(x => x.RecordedBy)
            .Include(x => x.FeeInvoice).ThenInclude(i => i.Student)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();

        return MapPaymentToDto(p);
    }

    private static PaymentResponseDto MapPaymentToDto(Payment p) => new()
    {
        Id = p.Id,
        ReceiptNumber = p.ReceiptNumber,
        FeeInvoiceId = p.FeeInvoiceId,
        InvoiceNumber = p.FeeInvoice.InvoiceNumber,
        StudentName = $"{p.FeeInvoice.Student.FirstName} {p.FeeInvoice.Student.LastName}",
        StudentNumber = p.FeeInvoice.Student.StudentNumber,
        Amount = p.Amount,
        PaymentDate = p.PaymentDate,
        PaymentMode = p.PaymentMode,
        ReferenceNumber = p.ReferenceNumber,
        Notes = p.Notes,
        RecordedByName = $"{p.RecordedBy.FirstName} {p.RecordedBy.LastName}",
        CreatedAt = p.CreatedAt
    };
}