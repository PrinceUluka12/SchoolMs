using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Hostel;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/hostel")]
[Authorize]
public class HostelController : ControllerBase
{
    private readonly IHostelService _service;

    public HostelController(IHostelService service) { _service = service; }

    // ── Buildings ─────────────────────────────────────────────────────────────
    [HttpGet("buildings")]
    public async Task<IActionResult> GetBuildings() => Ok(await _service.GetBuildingsAsync());

    [HttpGet("buildings/{id:guid}")]
    public async Task<IActionResult> GetBuilding(Guid id)
    {
        try { return Ok(await _service.GetBuildingByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("buildings")]
    [Authorize(Roles = "Admin,Warden")]
    public async Task<IActionResult> CreateBuilding([FromBody] CreateBuildingDto dto)
    {
        var result = await _service.CreateBuildingAsync(dto);
        return CreatedAtAction(nameof(GetBuilding), new { id = result.Id }, result);
    }

    [HttpPut("buildings/{id:guid}")]
    [Authorize(Roles = "Admin,Warden")]
    public async Task<IActionResult> UpdateBuilding(Guid id, [FromBody] UpdateBuildingDto dto)
    {
        try { return Ok(await _service.UpdateBuildingAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("buildings/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBuilding(Guid id)
    {
        try { await _service.DeleteBuildingAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ── Rooms ─────────────────────────────────────────────────────────────────
    [HttpGet("buildings/{buildingId:guid}/rooms")]
    public async Task<IActionResult> GetRooms(Guid buildingId)
        => Ok(await _service.GetRoomsByBuildingAsync(buildingId));

    [HttpGet("rooms/{id:guid}")]
    public async Task<IActionResult> GetRoom(Guid id)
    {
        try { return Ok(await _service.GetRoomByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("rooms")]
    [Authorize(Roles = "Admin,Warden")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomDto dto)
    {
        try
        {
            var result = await _service.CreateRoomAsync(dto);
            return CreatedAtAction(nameof(GetRoom), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("rooms/{id:guid}")]
    [Authorize(Roles = "Admin,Warden")]
    public async Task<IActionResult> UpdateRoom(Guid id, [FromBody] UpdateRoomDto dto)
    {
        try { return Ok(await _service.UpdateRoomAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("rooms/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        try { await _service.DeleteRoomAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ── Allocations ───────────────────────────────────────────────────────────
    [HttpPost("allocations")]
    [Authorize(Roles = "Admin,Warden")]
    public async Task<IActionResult> AllocateStudent([FromBody] AllocateStudentDto dto)
    {
        try { return Ok(await _service.AllocateStudentAsync(dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("allocations/{id:guid}")]
    [Authorize(Roles = "Admin,Warden")]
    public async Task<IActionResult> DeallocateStudent(Guid id)
    {
        try { await _service.DeallocateStudentAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("allocations")]
    [Authorize(Roles = "Admin,Warden")]
    public async Task<IActionResult> GetAllocationsByTerm([FromQuery] Guid termId)
        => Ok(await _service.GetAllocationsByTermAsync(termId));

    [HttpGet("allocations/student/{studentId:guid}")]
    [Authorize(Roles = "Admin,Warden,Student,Parent")]
    public async Task<IActionResult> GetStudentAllocation(Guid studentId, [FromQuery] Guid termId)
    {
        var result = await _service.GetStudentAllocationAsync(studentId, termId);
        if (result == null) return NotFound(new { message = "No hostel allocation found for this student." });
        return Ok(result);
    }
}