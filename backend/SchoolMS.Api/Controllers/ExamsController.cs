using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Exams;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/exams")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IExamService _service;

    public ExamsController(IExamService service) { _service = service; }

    // ── Exams ─────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> GetByTerm([FromQuery] Guid termId)
        => Ok(await _service.GetExamsByTermAsync(termId));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await _service.GetExamByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamDto dto)
    {
        var result = await _service.CreateExamAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateExam(Guid id, [FromBody] UpdateExamDto dto)
    {
        try { return Ok(await _service.UpdateExamAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteExam(Guid id)
    {
        try { await _service.DeleteExamAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ── Schedules ─────────────────────────────────────────────────────────────
    [HttpGet("{examId:guid}/schedules")]
    public async Task<IActionResult> GetSchedules(Guid examId)
        => Ok(await _service.GetSchedulesByExamAsync(examId));

    [HttpPost("schedules")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddSchedule([FromBody] CreateExamScheduleDto dto)
    {
        try { return Ok(await _service.AddScheduleAsync(dto)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("schedules/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        try { await _service.DeleteScheduleAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ── Seating ───────────────────────────────────────────────────────────────
    [HttpPost("schedules/{scheduleId:guid}/seating/generate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateSeating(Guid scheduleId)
    {
        try { return Ok(await _service.GenerateSeatingAsync(scheduleId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("schedules/{scheduleId:guid}/seating")]
    public async Task<IActionResult> GetSeating(Guid scheduleId)
    {
        try { return Ok(await _service.GetSeatingAsync(scheduleId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("schedules/{scheduleId:guid}/seating/pdf")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetSeatingPdf(Guid scheduleId)
    {
        try
        {
            var pdf = await _service.GenerateSeatingPdfAsync(scheduleId);
            return File(pdf, "application/pdf", $"seating-{scheduleId}.pdf");
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ── Admit Cards ───────────────────────────────────────────────────────────
    [HttpGet("{examId:guid}/admit-card/{studentId:guid}")]
    [Authorize(Roles = "Admin,Teacher,Student,Parent")]
    public async Task<IActionResult> GetAdmitCard(Guid examId, Guid studentId)
    {
        try
        {
            var pdf = await _service.GenerateAdmitCardPdfAsync(studentId, examId);
            return File(pdf, "application/pdf", $"admit-card-{studentId}.pdf");
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}