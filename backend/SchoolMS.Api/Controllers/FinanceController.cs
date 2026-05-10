using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Finance;
using SchoolMS.Core.Interfaces;
using System.Security.Claims;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/finance")]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly IFinanceService _service;
    private readonly IAttendanceService _attendanceService;

    public FinanceController(IFinanceService service, IAttendanceService attendanceService)
    {
        _service = service;
        _attendanceService = attendanceService;
    }

    // ── Fee Categories ────────────────────────────────────────────────────────
    [HttpGet("categories")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetCategories()
        => Ok(await _service.GetCategoriesAsync());

    [HttpGet("categories/{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        try { return Ok(await _service.GetCategoryByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("categories")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateFeeCategoryDto dto)
    {
        try
        {
            var result = await _service.CreateCategoryAsync(dto);
            return CreatedAtAction(nameof(GetCategory), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("categories/{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateFeeCategoryDto dto)
    {
        try { return Ok(await _service.UpdateCategoryAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("categories/{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        try { await _service.DeleteCategoryAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ── Fee Structures ────────────────────────────────────────────────────────
    [HttpGet("structures")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetStructures(
        [FromQuery] Guid? termId, [FromQuery] Guid? classId)
        => Ok(await _service.GetStructuresAsync(termId, classId));

    [HttpGet("structures/{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetStructure(Guid id)
    {
        try { return Ok(await _service.GetStructureByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("structures")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> CreateStructure([FromBody] CreateFeeStructureDto dto)
    {
        try
        {
            var result = await _service.CreateStructureAsync(dto);
            return CreatedAtAction(nameof(GetStructure), new { id = result.Id }, result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("structures/{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> UpdateStructure(Guid id, [FromBody] UpdateFeeStructureDto dto)
    {
        try { return Ok(await _service.UpdateStructureAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("structures/{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> DeleteStructure(Guid id)
    {
        try { await _service.DeleteStructureAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ── Invoices ──────────────────────────────────────────────────────────────
    [HttpPost("invoices/generate")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GenerateInvoices([FromBody] GenerateInvoicesDto dto)
    {
        try
        {
            var result = await _service.GenerateInvoicesAsync(dto);
            return Ok(new { generated = result.Count, invoices = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("invoices/{id:guid}")]
    [Authorize(Roles = "Admin,Finance,Parent")]
    public async Task<IActionResult> GetInvoice(Guid id)
    {
        try { return Ok(await _service.GetInvoiceByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("invoices/class/{classId:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetClassInvoices(Guid classId, [FromQuery] Guid termId)
        => Ok(await _service.GetInvoicesByClassAsync(classId, termId));

    [HttpGet("invoices/overdue")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetOverdue([FromQuery] Guid termId)
        => Ok(await _service.GetOverdueInvoicesAsync(termId));

    [HttpGet("student/{studentId:guid}/statement")]
    [Authorize(Roles = "Admin,Finance,Parent,Student")]
    public async Task<IActionResult> GetStudentStatement(
        Guid studentId, [FromQuery] Guid? termId)
    {
        try
        {
            var result = await _service.GetStudentStatementAsync(studentId, termId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPatch("invoices/{id:guid}/discount")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> ApplyDiscount(Guid id, [FromBody] ApplyDiscountDto dto)
    {
        try { return Ok(await _service.ApplyDiscountAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("invoices/{id:guid}/waive")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> WaiveInvoice(Guid id, [FromBody] string notes)
    {
        try { await _service.WaiveInvoiceAsync(id, notes); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ── Payments ──────────────────────────────────────────────────────────────
    [HttpPost("payments")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentDto dto)
    {
        try
        {
            var staffId = await GetStaffIdAsync();
            var result = await _service.RecordPaymentAsync(dto, staffId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("payments/{id:guid}")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        try { return Ok(await _service.GetPaymentByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("payments/{id:guid}/receipt")]
    [Authorize(Roles = "Admin,Finance,Parent")]
    public async Task<IActionResult> GetReceipt(Guid id)
    {
        try
        {
            var pdf = await _service.GenerateReceiptPdfAsync(id);
            return File(pdf, "application/pdf", $"receipt-{id}.pdf");
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("invoices/{invoiceId:guid}/payments")]
    [Authorize(Roles = "Admin,Finance,Parent")]
    public async Task<IActionResult> GetPaymentsByInvoice(Guid invoiceId)
        => Ok(await _service.GetPaymentsByInvoiceAsync(invoiceId));

    // ── Reports ───────────────────────────────────────────────────────────────
    [HttpGet("summary")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid termId)
    {
        try { return Ok(await _service.GetFinanceSummaryAsync(termId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private async Task<Guid> GetStaffIdAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _attendanceService.GetStaffByUserIdAsync(userId);
    }
}