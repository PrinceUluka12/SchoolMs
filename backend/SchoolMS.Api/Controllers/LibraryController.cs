using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Library;
using SchoolMS.Core.Interfaces;
using System.Security.Claims;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/library")]
[Authorize]
public class LibraryController : ControllerBase
{
    private readonly ILibraryService _service;
    private readonly IAttendanceService _attendanceService;

    public LibraryController(ILibraryService service, IAttendanceService attendanceService)
    {
        _service = service;
        _attendanceService = attendanceService;
    }

    // GET /api/v1/library/books
    [HttpGet("books")]
    public async Task<IActionResult> SearchBooks(
        [FromQuery] string? search, [FromQuery] string? category)
        => Ok(await _service.SearchBooksAsync(search, category));

    // GET /api/v1/library/books/{id}
    [HttpGet("books/{id:guid}")]
    public async Task<IActionResult> GetBook(Guid id)
    {
        try { return Ok(await _service.GetBookByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // POST /api/v1/library/books
    [HttpPost("books")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> AddBook([FromBody] CreateBookDto dto)
    {
        try
        {
            var result = await _service.AddBookAsync(dto);
            return CreatedAtAction(nameof(GetBook), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // PUT /api/v1/library/books/{id}
    [HttpPut("books/{id:guid}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> UpdateBook(Guid id, [FromBody] UpdateBookDto dto)
    {
        try { return Ok(await _service.UpdateBookAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // DELETE /api/v1/library/books/{id}
    [HttpDelete("books/{id:guid}")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> DeleteBook(Guid id)
    {
        try { await _service.DeleteBookAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // POST /api/v1/library/loans
    [HttpPost("loans")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> IssueLoan([FromBody] IssueLoanDto dto)
    {
        try
        {
            var staffId = await GetStaffIdAsync();
            var result = await _service.IssueLoanAsync(dto, staffId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // PATCH /api/v1/library/loans/{id}/return
    [HttpPatch("loans/{id:guid}/return")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> ReturnLoan(Guid id, [FromBody] ReturnLoanDto dto)
    {
        try { return Ok(await _service.ReturnLoanAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // GET /api/v1/library/loans/active
    [HttpGet("loans/active")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetActiveLoans()
        => Ok(await _service.GetAllActiveLoansAsync());

    // GET /api/v1/library/loans/overdue
    [HttpGet("loans/overdue")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetOverdueLoans()
        => Ok(await _service.GetOverdueLoansAsync());

    // GET /api/v1/library/loans/borrower/{borrowerUserId}
    [HttpGet("loans/borrower/{borrowerUserId:guid}")]
    [Authorize(Roles = "Admin,Librarian,Teacher,Student")]
    public async Task<IActionResult> GetLoansByBorrower(Guid borrowerUserId)
        => Ok(await _service.GetActiveLoansByBorrowerAsync(borrowerUserId));

    // GET /api/v1/library/stats
    [HttpGet("stats")]
    [Authorize(Roles = "Admin,Librarian")]
    public async Task<IActionResult> GetStats()
        => Ok(await _service.GetStatsAsync());

    private async Task<Guid> GetStaffIdAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _attendanceService.GetStaffByUserIdAsync(userId);
    }
}