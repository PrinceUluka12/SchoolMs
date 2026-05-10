using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Students;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/guardians")]
[Authorize(Roles = "Admin")]
public class GuardiansController : ControllerBase
{
    private readonly IGuardianService _guardianService;

    public GuardiansController(IGuardianService guardianService)
    {
        _guardianService = guardianService;
    }

    // GET /api/v1/guardians
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _guardianService.GetAllAsync();
        return Ok(result);
    }

    // GET /api/v1/guardians/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _guardianService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // POST /api/v1/guardians
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGuardianDto dto)
    {
        try
        {
            var result = await _guardianService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // PUT /api/v1/guardians/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGuardianDto dto)
    {
        try
        {
            var result = await _guardianService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // DELETE /api/v1/guardians/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _guardianService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // GET /api/v1/guardians/{id}/students
    [HttpGet("{id:guid}/students")]
    public async Task<IActionResult> GetStudents(Guid id)
    {
        try
        {
            var result = await _guardianService.GetStudentsByGuardianAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}