using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Communication;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class CommunicationService : ICommunicationService
{
    private readonly AppDbContext _db;

    public CommunicationService(AppDbContext db)
    {
        _db = db;
    }

    // ── Messages ──────────────────────────────────────────────────────────────
    public async Task<MessageResponseDto> SendMessageAsync(SendMessageDto dto, Guid senderId)
    {
        var recipientExists = await _db.Users.AnyAsync(u => u.Id == dto.RecipientId);
        if (!recipientExists)
            throw new KeyNotFoundException($"Recipient {dto.RecipientId} not found.");

        var message = new Message
        {
            SenderId = senderId,
            RecipientId = dto.RecipientId,
            Subject = dto.Subject.Trim(),
            Body = dto.Body.Trim(),
            Type = "Direct",
            IsRead = false
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();
        return await MapMessageAsync(message.Id);
    }

    public async Task<MessageResponseDto> SendClassBroadcastAsync(
        SendClassBroadcastDto dto, Guid senderId)
    {
        var classExists = await _db.Classes.AnyAsync(c => c.Id == dto.ClassId);
        if (!classExists)
            throw new KeyNotFoundException($"Class {dto.ClassId} not found.");

        // Get all guardian userIds for students in this class
        var guardianUserIds = await _db.Students
            .Include(s => s.Guardian)
            .Where(s => s.ClassId == dto.ClassId && s.Status == "Active")
            .Select(s => s.Guardian.UserId)
            .Distinct()
            .ToListAsync();

        var messages = guardianUserIds.Select(recipientId => new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            ClassId = dto.ClassId.ToString(),
            Subject = dto.Subject.Trim(),
            Body = dto.Body.Trim(),
            Type = "ClassBroadcast",
            IsRead = false
        }).ToList();

        _db.Messages.AddRange(messages);
        await _db.SaveChangesAsync();

        // Return the first one as representative
        return await MapMessageAsync(messages.First().Id);
    }

    public async Task<InboxResponseDto> GetInboxAsync(Guid userId)
    {
        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Where(m =>
                m.RecipientId == userId &&
                !m.IsDeletedByRecipient)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return new InboxResponseDto
        {
            UnreadCount = messages.Count(m => !m.IsRead),
            Messages = messages.Select(MapMessageToDto).ToList()
        };
    }

    public async Task<IEnumerable<MessageResponseDto>> GetSentAsync(Guid userId)
    {
        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .Where(m =>
                m.SenderId == userId &&
                !m.IsDeletedBySender)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(MapMessageToDto);
    }

    public async Task<MessageResponseDto> GetMessageAsync(Guid messageId, Guid userId)
    {
        var message = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Recipient)
            .FirstOrDefaultAsync(m => m.Id == messageId &&
                (m.RecipientId == userId || m.SenderId == userId))
            ?? throw new KeyNotFoundException($"Message {messageId} not found.");

        return MapMessageToDto(message);
    }

    public async Task MarkAsReadAsync(Guid messageId, Guid userId)
    {
        var message = await _db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId && m.RecipientId == userId)
            ?? throw new KeyNotFoundException($"Message {messageId} not found.");

        if (!message.IsRead)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    public async Task DeleteMessageAsync(Guid messageId, Guid userId)
    {
        var message = await _db.Messages
            .FirstOrDefaultAsync(m => m.Id == messageId &&
                (m.RecipientId == userId || m.SenderId == userId))
            ?? throw new KeyNotFoundException($"Message {messageId} not found.");

        if (message.RecipientId == userId)
            message.IsDeletedByRecipient = true;
        else
            message.IsDeletedBySender = true;

        await _db.SaveChangesAsync();
    }

    // ── Announcements ─────────────────────────────────────────────────────────
    public async Task<AnnouncementResponseDto> CreateAnnouncementAsync(
        CreateAnnouncementDto dto, Guid createdById)
    {
        var announcement = new Announcement
        {
            Title = dto.Title.Trim(),
            Body = dto.Body.Trim(),
            Audience = dto.Audience,
            IsPinned = dto.IsPinned,
            ExpiresAt = dto.ExpiresAt,
            CreatedById = createdById
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();
        return await MapAnnouncementAsync(announcement.Id);
    }

    public async Task<AnnouncementResponseDto> UpdateAnnouncementAsync(
        Guid id, UpdateAnnouncementDto dto)
    {
        var announcement = await _db.Announcements.FindAsync(id)
            ?? throw new KeyNotFoundException($"Announcement {id} not found.");

        if (dto.Title != null) announcement.Title = dto.Title.Trim();
        if (dto.Body != null) announcement.Body = dto.Body.Trim();
        if (dto.Audience != null) announcement.Audience = dto.Audience;
        if (dto.IsPinned.HasValue) announcement.IsPinned = dto.IsPinned.Value;
        if (dto.ExpiresAt.HasValue) announcement.ExpiresAt = dto.ExpiresAt;

        await _db.SaveChangesAsync();
        return await MapAnnouncementAsync(id);
    }

    public async Task DeleteAnnouncementAsync(Guid id)
    {
        var announcement = await _db.Announcements.FindAsync(id)
            ?? throw new KeyNotFoundException($"Announcement {id} not found.");

        announcement.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<AnnouncementResponseDto>> GetAnnouncementsAsync(string? audience)
    {
        var query = _db.Announcements
            .Include(a => a.CreatedBy)
            .Where(a => a.ExpiresAt == null || a.ExpiresAt > DateTime.UtcNow);

        if (!string.IsNullOrEmpty(audience) && audience != "All")
            query = query.Where(a => a.Audience == "All" || a.Audience == audience);

        var announcements = await query
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return announcements.Select(MapAnnouncementToDto);
    }

    public async Task<AnnouncementResponseDto> GetAnnouncementByIdAsync(Guid id)
    {
        var exists = await _db.Announcements.AnyAsync(a => a.Id == id);
        if (!exists) throw new KeyNotFoundException($"Announcement {id} not found.");
        return await MapAnnouncementAsync(id);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task<MessageResponseDto> MapMessageAsync(Guid id)
    {
        var m = await _db.Messages
            .Include(x => x.Sender)
            .Include(x => x.Recipient)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapMessageToDto(m);
    }

    private static MessageResponseDto MapMessageToDto(Message m) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        SenderName = $"{m.Sender.Email}",
        SenderRole = m.Sender.Role,
        RecipientId = m.RecipientId,
        RecipientName = m.Recipient?.Email,
        Subject = m.Subject,
        Body = m.Body,
        Type = m.Type,
        IsRead = m.IsRead,
        ReadAt = m.ReadAt,
        CreatedAt = m.CreatedAt
    };

    private async Task<AnnouncementResponseDto> MapAnnouncementAsync(Guid id)
    {
        var a = await _db.Announcements
            .Include(x => x.CreatedBy)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapAnnouncementToDto(a);
    }

    private static AnnouncementResponseDto MapAnnouncementToDto(Announcement a) => new()
    {
        Id = a.Id,
        Title = a.Title,
        Body = a.Body,
        Audience = a.Audience,
        IsPinned = a.IsPinned,
        ExpiresAt = a.ExpiresAt,
        CreatedById = a.CreatedById,
        CreatedByName = a.CreatedBy?.Email ?? "System",
        CreatedAt = a.CreatedAt
    };
}