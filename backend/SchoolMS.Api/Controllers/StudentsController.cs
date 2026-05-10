using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Common;
using SchoolMS.Core.DTOs.Students;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    // GET /api/v1/students?search=john&classId=...&page=1&pageSize=20
    [HttpGet]
    [Authorize(Roles = "Admin,Teacher,Finance")]
    public async Task<IActionResult> GetAll([FromQuery] StudentFilterDto filter)
    {
        var result = await _studentService.GetAllAsync(filter);
        return Ok(result);
    }

    // GET /api/v1/students/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Teacher,Finance,Parent,Student")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _studentService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // POST /api/v1/students
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateStudentDto dto)
    {
        try
        {
            var result = await _studentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // PUT /api/v1/students/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStudentDto dto)
    {
        try
        {
            var result = await _studentService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // DELETE /api/v1/students/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _studentService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // PATCH /api/v1/students/{id}/class
    [HttpPatch("{id:guid}/class")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignClass(Guid id, [FromBody] Guid classId)
    {
        try
        {
            var result = await _studentService.AssignClassAsync(id, classId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // PATCH /api/v1/students/{id}/transfer
    [HttpPatch("{id:guid}/transfer")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Transfer(Guid id, [FromBody] TransferStudentDto dto)
    {
        try
        {
            var result = await _studentService.TransferAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // PATCH /api/v1/students/{id}/status
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
    {
        try
        {
            var result = await _studentService.UpdateStatusAsync(id, status);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // POST /api/v1/students/{id}/photo
    [HttpPost("{id:guid}/photo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
    {
        try
        {
            var url = await _studentService.UploadPhotoAsync(id, file);
            return Ok(new { photoUrl = url });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // POST /api/v1/students/{id}/documents
    [HttpPost("{id:guid}/documents")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] UploadDocumentDto dto)
    {
        try
        {
            var result = await _studentService.UploadDocumentAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    // GET /api/v1/students/{id}/documents
    [HttpGet("{id:guid}/documents")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetDocuments(Guid id)
    {
        var result = await _studentService.GetDocumentsAsync(id);
        return Ok(result);
    }

    // DELETE /api/v1/students/{id}/documents/{documentId}
    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid documentId)
    {
        try
        {
            await _studentService.DeleteDocumentAsync(documentId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // POST /api/v1/students/bulk-import
    [HttpPost("bulk-import")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkImport(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
            return BadRequest(new { message = "CSV file is required." });

        var ext = Path.GetExtension(csvFile.FileName).ToLowerInvariant();
        if (ext != ".csv")
            return BadRequest(new { message = "File must be a .csv" });

        var result = await _studentService.BulkImportAsync(csvFile);
        return Ok(result);
    }

    // GET /api/v1/students/{id}/id-card
    [HttpGet("{id:guid}/id-card")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetIdCard(Guid id)
    {
        try
        {
            var bytes = await _studentService.GenerateStudentIdCardAsync(id);
            return File(bytes, "text/plain", $"student-{id}-idcard.txt");
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}