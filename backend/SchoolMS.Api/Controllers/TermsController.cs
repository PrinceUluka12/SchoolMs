using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Academic;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/terms")]
[Authorize]
public class TermsController : ControllerBase
{
    private readonly ITermService _service;

    public TermsController(ITermService service)
    {
        _service = service;
    }

    // GET /api/v1/terms/current
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var result = await _service.GetCurrentAsync();
        if (result == null) return NotFound(new { message = "No current term set." });
        return Ok(result);
    }

    // GET /api/v1/terms/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try { return Ok(await _service.GetByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // GET /api/v1/terms?academicYearId=...
    [HttpGet]
    public async Task<IActionResult> GetByAcademicYear([FromQuery] Guid academicYearId)
        => Ok(await _service.GetByAcademicYearAsync(academicYearId));

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateTermDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTermDto dto)
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

    // PATCH /api/v1/terms/{id}/set-current
    [HttpPatch("{id:guid}/set-current")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetCurrent(Guid id)
    {
        try { return Ok(await _service.SetCurrentAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}