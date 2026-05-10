using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Attendance;
using SchoolMS.Core.Interfaces;
using System.Security.Claims;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _service;

    public AttendanceController(IAttendanceService service)
    {
        _service = service;
    }

    // GET /api/v1/attendance/register?classId=...&date=2026-05-01&period=1
    [HttpGet("register")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetRegister(
        [FromQuery] Guid classId,
        [FromQuery] DateTime date,
        [FromQuery] int? period)
    {
        try
        {
            var result = await _service.GetRegisterAsync(classId, date, period);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // POST /api/v1/attendance/mark
    [HttpPost("mark")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> MarkBulk([FromBody] MarkAttendanceDto dto)
    {
        try
        {
            var staffId = await GetStaffIdFromUserAsync();
            var result = await _service.MarkBulkAsync(dto, staffId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // PATCH /api/v1/attendance/{id}
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAttendanceDto dto)
    {
        try
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // GET /api/v1/attendance/student/{studentId}/summary
    [HttpGet("student/{studentId:guid}/summary")]
    [Authorize(Roles = "Admin,Teacher,Parent,Student")]
    public async Task<IActionResult> GetStudentSummary(
        Guid studentId,
        [FromQuery] Guid? termId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _service.GetStudentSummaryAsync(studentId, termId, from, to);
        return Ok(result);
    }

    // GET /api/v1/attendance/class/{classId}/report?from=...&to=...
    [HttpGet("class/{classId:guid}/report")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetClassReport(
        Guid classId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        try
        {
            var result = await _service.GetClassReportAsync(classId, from, to);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // GET /api/v1/attendance/class/{classId}/low?termId=...&threshold=75
    [HttpGet("class/{classId:guid}/low")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetLowAttendance(
        Guid classId,
        [FromQuery] Guid termId,
        [FromQuery] decimal threshold = 75)
    {
        var result = await _service.GetLowAttendanceAsync(classId, termId, threshold);
        return Ok(result);
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private async Task<Guid> GetStaffIdFromUserAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var staff = await _service.GetStaffByUserIdAsync(userId);
        return staff;
    }
}