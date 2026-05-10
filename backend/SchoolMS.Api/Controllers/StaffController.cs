using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Common;
using SchoolMS.Core.DTOs.Staff;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/staff")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    // GET /api/v1/staff
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] StaffFilterDto filter)
    {
        var result = await _staffService.GetAllAsync(filter);
        return Ok(result);
    }

    // GET /api/v1/staff/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _staffService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // POST /api/v1/staff
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateStaffDto dto)
    {
        try
        {
            var result = await _staffService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // PUT /api/v1/staff/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffDto dto)
    {
        try
        {
            var result = await _staffService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // DELETE /api/v1/staff/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _staffService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // PATCH /api/v1/staff/{id}/status
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
    {
        try
        {
            var result = await _staffService.UpdateStatusAsync(id, status);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // POST /api/v1/staff/{id}/photo
    [HttpPost("{id:guid}/photo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
    {
        try
        {
            var url = await _staffService.UploadPhotoAsync(id, file);
            return Ok(new { photoUrl = url });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // POST /api/v1/staff/{id}/documents
    [HttpPost("{id:guid}/documents")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] UploadStaffDocumentDto dto)
    {
        try
        {
            var result = await _staffService.UploadDocumentAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // GET /api/v1/staff/{id}/documents
    [HttpGet("{id:guid}/documents")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDocuments(Guid id)
    {
        var result = await _staffService.GetDocumentsAsync(id);
        return Ok(result);
    }

    // DELETE /api/v1/staff/{id}/documents/{documentId}
    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid documentId)
    {
        try
        {
            await _staffService.DeleteDocumentAsync(documentId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}