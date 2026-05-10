using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Grades;
using SchoolMS.Core.Interfaces;
using System.Security.Claims;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/grades")]
[Authorize]
public class GradesController : ControllerBase
{
    private readonly IGradeService _service;
    private readonly IAttendanceService _attendanceService;

    public GradesController(IGradeService service, IAttendanceService attendanceService)
    {
        _service = service;
        _attendanceService = attendanceService;
    }

    // POST /api/v1/grades
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> EnterGrade([FromBody] EnterGradeDto dto)
    {
        try
        {
            var staffId = await GetStaffIdAsync();
            var result = await _service.EnterGradeAsync(dto, staffId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    // POST /api/v1/grades/bulk
    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> BulkEnterGrades([FromBody] BulkEnterGradesDto dto)
    {
        try
        {
            var staffId = await GetStaffIdAsync();
            var result = await _service.BulkEnterGradesAsync(dto, staffId);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // PUT /api/v1/grades/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> UpdateGrade(Guid id, [FromBody] UpdateGradeRequestDto dto)
    {
        try
        {
            var staffId = await GetStaffIdAsync();
            var result = await _service.UpdateGradeAsync(id, dto.Score, dto.TeacherRemark, staffId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // DELETE /api/v1/grades/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> DeleteGrade(Guid id)
    {
        try
        {
            await _service.DeleteGradeAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // GET /api/v1/grades/sheet?classId=...&termId=...&subjectId=...&assessmentType=...
    [HttpGet("sheet")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetGradeSheet(
        [FromQuery] Guid classId,
        [FromQuery] Guid termId,
        [FromQuery] Guid subjectId,
        [FromQuery] string assessmentType)
    {
        try
        {
            var result = await _service.GetClassGradeSheetAsync(classId, termId, subjectId, assessmentType);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // GET /api/v1/grades/student/{studentId}/report?termId=...
    [HttpGet("student/{studentId:guid}/report")]
    [Authorize(Roles = "Admin,Teacher,Parent,Student")]
    public async Task<IActionResult> GetStudentReport(Guid studentId, [FromQuery] Guid termId)
    {
        try
        {
            var result = await _service.GetStudentTermReportAsync(studentId, termId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // GET /api/v1/grades/class/{classId}/reports?termId=...
    [HttpGet("class/{classId:guid}/reports")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetClassReports(Guid classId, [FromQuery] Guid termId)
    {
        try
        {
            var result = await _service.GetClassTermReportsAsync(classId, termId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // POST /api/v1/grades/lock?classId=...&termId=...
    [HttpPost("lock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Lock([FromQuery] Guid classId, [FromQuery] Guid termId)
    {
        await _service.LockGradesAsync(classId, termId);
        return Ok(new { message = "Grades locked successfully." });
    }

    // POST /api/v1/grades/unlock?classId=...&termId=...
    [HttpPost("unlock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Unlock([FromQuery] Guid classId, [FromQuery] Guid termId)
    {
        await _service.UnlockGradesAsync(classId, termId);
        return Ok(new { message = "Grades unlocked successfully." });
    }

    // GET /api/v1/grades/report-card/{studentId}?termId=...
    [HttpGet("report-card/{studentId:guid}")]
    [Authorize(Roles = "Admin,Teacher,Parent,Student")]
    public async Task<IActionResult> GetReportCard(Guid studentId, [FromQuery] Guid termId)
    {
        try
        {
            var pdf = await _service.GenerateReportCardPdfAsync(studentId, termId);
            return File(pdf, "application/pdf", $"report-card-{studentId}-{termId}.pdf");
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // GET /api/v1/grades/report-cards/class/{classId}?termId=...
    [HttpGet("report-cards/class/{classId:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetClassReportCards(Guid classId, [FromQuery] Guid termId)
    {
        try
        {
            var pdf = await _service.GenerateClassReportCardsPdfAsync(classId, termId);
            return File(pdf, "application/pdf", $"report-cards-class-{classId}-{termId}.pdf");
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // POST /api/v1/grades/weights
    [HttpPost("weights")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetWeights([FromBody] SetAssessmentWeightDto dto)
    {
        try
        {
            await _service.SetAssessmentWeightsAsync(dto);
            return Ok(new { message = "Assessment weights saved." });
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // GET /api/v1/grades/weights?classId=...&termId=...
    [HttpGet("weights")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetWeights([FromQuery] Guid classId, [FromQuery] Guid termId)
    {
        var result = await _service.GetAssessmentWeightsAsync(classId, termId);
        return Ok(result);
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private async Task<Guid> GetStaffIdAsync()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _attendanceService.GetStaffByUserIdAsync(userId);
    }
}

// ── Request DTO ───────────────────────────────────────────────────────────────
public class UpdateGradeRequestDto
{
    public decimal Score { get; set; }
    public string? TeacherRemark { get; set; }
}