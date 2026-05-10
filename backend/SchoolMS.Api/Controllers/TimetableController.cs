using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Timetable;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/timetable")]
[Authorize]
public class TimetableController : ControllerBase
{
    private readonly ITimetableService _service;

    public TimetableController(ITimetableService service)
    {
        _service = service;
    }

    // GET /api/v1/timetable/slots?academicYearId=...&classId=...
    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots(
        [FromQuery] Guid academicYearId,
        [FromQuery] Guid? classId)
    {
        var result = await _service.GetAllSlotsAsync(academicYearId, classId);
        return Ok(result);
    }

    // GET /api/v1/timetable/class/{classId}?academicYearId=...
    [HttpGet("class/{classId:guid}")]
    public async Task<IActionResult> GetClassTimetable(Guid classId, [FromQuery] Guid academicYearId)
    {
        try
        {
            var result = await _service.GetClassTimetableAsync(classId, academicYearId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // GET /api/v1/timetable/teacher/{teacherId}?academicYearId=...
    [HttpGet("teacher/{teacherId:guid}")]
    public async Task<IActionResult> GetTeacherTimetable(Guid teacherId, [FromQuery] Guid academicYearId)
    {
        try
        {
            var result = await _service.GetTeacherTimetableAsync(teacherId, academicYearId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // POST /api/v1/timetable/slots
    [HttpPost("slots")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateSlot([FromBody] CreateTimetableSlotDto dto)
    {
        try
        {
            var result = await _service.CreateSlotAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // POST /api/v1/timetable/slots/check-conflict
    [HttpPost("slots/check-conflict")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CheckConflict([FromBody] CreateTimetableSlotDto dto)
    {
        var result = await _service.CheckConflictAsync(dto);
        return Ok(result);
    }

    // PUT /api/v1/timetable/slots/{id}
    [HttpPut("slots/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSlot(Guid id, [FromBody] UpdateTimetableSlotDto dto)
    {
        try
        {
            var result = await _service.UpdateSlotAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // DELETE /api/v1/timetable/slots/{id}
    [HttpDelete("slots/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSlot(Guid id)
    {
        try
        {
            await _service.DeleteSlotAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}