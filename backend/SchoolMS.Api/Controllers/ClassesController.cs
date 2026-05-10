using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Academic;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/classes")]
[Authorize]
public class ClassesController : ControllerBase
{
    private readonly IClassService _service;

    public ClassesController(IClassService service)
    {
        _service = service;
    }

    // GET /api/v1/classes?academicYearId=...
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? academicYearId)
        => Ok(await _service.GetAllAsync(academicYearId));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await _service.GetByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateClassDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassDto dto)
    {
        try { return Ok(await _service.UpdateAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await _service.DeleteAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // PATCH /api/v1/classes/{id}/teacher
    [HttpPatch("{id:guid}/teacher")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignTeacher(Guid id, [FromBody] Guid teacherId)
    {
        try { return Ok(await _service.AssignTeacherAsync(id, teacherId)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // GET /api/v1/classes/{id}/subjects
    [HttpGet("{id:guid}/subjects")]
    public async Task<IActionResult> GetSubjects(Guid id)
        => Ok(await _service.GetSubjectsAsync(id));

    // POST /api/v1/classes/{id}/subjects
    [HttpPost("{id:guid}/subjects")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignSubject(Guid id, [FromBody] AssignSubjectDto dto)
    {
        try { return Ok(await _service.AssignSubjectAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // DELETE /api/v1/classes/{id}/subjects/{subjectId}
    [HttpDelete("{id:guid}/subjects/{subjectId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveSubject(Guid id, Guid subjectId)
    {
        try { await _service.RemoveSubjectAsync(id, subjectId); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}