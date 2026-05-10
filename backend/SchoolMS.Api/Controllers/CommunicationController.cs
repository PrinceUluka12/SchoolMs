using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolMS.Core.DTOs.Communication;
using SchoolMS.Core.Interfaces;
using System.Security.Claims;

namespace SchoolMS.Api.Controllers;

[ApiController]
[Route("api/v1/communications")]
[Authorize]
public class CommunicationController : ControllerBase
{
    private readonly ICommunicationService _service;

    public CommunicationController(ICommunicationService service)
    {
        _service = service;
    }

    // ── Messages ──────────────────────────────────────────────────────────────
    [HttpGet("inbox")]
    public async Task<IActionResult> GetInbox()
    {
        var userId = GetUserId();
        return Ok(await _service.GetInboxAsync(userId));
    }

    [HttpGet("sent")]
    public async Task<IActionResult> GetSent()
    {
        var userId = GetUserId();
        return Ok(await _service.GetSentAsync(userId));
    }

    [HttpGet("messages/{id:guid}")]
    public async Task<IActionResult> GetMessage(Guid id)
    {
        try
        {
            var userId = GetUserId();
            var result = await _service.GetMessageAsync(id, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        try
        {
            var senderId = GetUserId();
            var result = await _service.SendMessageAsync(dto, senderId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("messages/broadcast")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> SendBroadcast([FromBody] SendClassBroadcastDto dto)
    {
        try
        {
            var senderId = GetUserId();
            var result = await _service.SendClassBroadcastAsync(dto, senderId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPatch("messages/{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        try
        {
            await _service.MarkAsReadAsync(id, GetUserId());
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("messages/{id:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid id)
    {
        try
        {
            await _service.DeleteMessageAsync(id, GetUserId());
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ── Announcements ─────────────────────────────────────────────────────────
    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements([FromQuery] string? audience)
        => Ok(await _service.GetAnnouncementsAsync(audience));

    [HttpGet("announcements/{id:guid}")]
    public async Task<IActionResult> GetAnnouncement(Guid id)
    {
        try { return Ok(await _service.GetAnnouncementByIdAsync(id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("announcements")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] CreateAnnouncementDto dto)
    {
        var createdById = GetUserId();
        var result = await _service.CreateAnnouncementAsync(dto, createdById);
        return CreatedAtAction(nameof(GetAnnouncement), new { id = result.Id }, result);
    }

    [HttpPut("announcements/{id:guid}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> UpdateAnnouncement(Guid id, [FromBody] UpdateAnnouncementDto dto)
    {
        try { return Ok(await _service.UpdateAnnouncementAsync(id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpDelete("announcements/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAnnouncement(Guid id)
    {
        try { await _service.DeleteAnnouncementAsync(id); return NoContent(); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}