using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Transport;
using SchoolMS.Core.Interfaces;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/transport")]
[Authorize]
public class TransportController : ControllerBase
{
    private readonly ITransportService _service;

    public TransportController(ITransportService service) { _service = service; }

    // ── Vehicles ──────────────────────────────────────────────────────────────
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles() => Ok(await _service.GetVehiclesAsync());

    [HttpGet("vehicles/{id:guid}")]
    public async Task<IActionResult> GetVehicle(Guid id)
    {
        try { return Ok(await _service.GetVehicleByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("vehicles")]
    [Authorize(Roles = "Admin,Transport")]
    public async Task<IActionResult> CreateVehicle([FromBody] CreateVehicleDto dto)
    {
        try
        {
            var result = await _service.CreateVehicleAsync(dto);
            return CreatedAtAction(nameof(GetVehicle), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("vehicles/{id:guid}")]
    [Authorize(Roles = "Admin,Transport")]
    public async Task<IActionResult> UpdateVehicle(Guid id, [FromBody] UpdateVehicleDto dto)
    {
        try { return Ok(await _service.UpdateVehicleAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("vehicles/{id:guid}")]
    [Authorize(Roles = "Admin,Transport")]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        try { await _service.DeleteVehicleAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ── Routes ────────────────────────────────────────────────────────────────
    [HttpGet("routes")]
    public async Task<IActionResult> GetRoutes() => Ok(await _service.GetRoutesAsync());

    [HttpGet("routes/{id:guid}")]
    public async Task<IActionResult> GetRoute(Guid id)
    {
        try { return Ok(await _service.GetRouteByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("routes")]
    [Authorize(Roles = "Admin,Transport")]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteDto dto)
    {
        var result = await _service.CreateRouteAsync(dto);
        return CreatedAtAction(nameof(GetRoute), new { id = result.Id }, result);
    }

    [HttpPut("routes/{id:guid}")]
    [Authorize(Roles = "Admin,Transport")]
    public async Task<IActionResult> UpdateRoute(Guid id, [FromBody] UpdateRouteDto dto)
    {
        try { return Ok(await _service.UpdateRouteAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("routes/{id:guid}")]
    [Authorize(Roles = "Admin,Transport")]
    public async Task<IActionResult> DeleteRoute(Guid id)
    {
        try { await _service.DeleteRouteAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    // ── Student Transport ─────────────────────────────────────────────────────
    [HttpPost("assignments")]
    [Authorize(Roles = "Admin,Transport")]
    public async Task<IActionResult> AssignStudent([FromBody] AssignStudentTransportDto dto)
    {
        try { return Ok(await _service.AssignStudentAsync(dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("routes/{routeId:guid}/students")]
    [Authorize(Roles = "Admin,Transport")]
    public async Task<IActionResult> GetStudentsByRoute(Guid routeId, [FromQuery] Guid termId)
        => Ok(await _service.GetStudentsByRouteAsync(routeId, termId));

    [HttpDelete("assignments/{id:guid}")]
    [Authorize(Roles = "Admin,Transport")]
    public async Task<IActionResult> RemoveAssignment(Guid id)
    {
        try { await _service.RemoveStudentFromRouteAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}