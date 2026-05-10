using SchoolMS.Core.DTOs.Communication;

namespace SchoolMS.Core.Interfaces;

public interface ICommunicationService
{
    // Messages
    Task<MessageResponseDto> SendMessageAsync(SendMessageDto dto, Guid senderId);
    Task<MessageResponseDto> SendClassBroadcastAsync(SendClassBroadcastDto dto, Guid senderId);
    Task<InboxResponseDto> GetInboxAsync(Guid userId);
    Task<IEnumerable<MessageResponseDto>> GetSentAsync(Guid userId);
    Task<MessageResponseDto> GetMessageAsync(Guid messageId, Guid userId);
    Task MarkAsReadAsync(Guid messageId, Guid userId);
    Task DeleteMessageAsync(Guid messageId, Guid userId);

    // Announcements
    Task<AnnouncementResponseDto> CreateAnnouncementAsync(CreateAnnouncementDto dto, Guid createdById);
    Task<AnnouncementResponseDto> UpdateAnnouncementAsync(Guid id, UpdateAnnouncementDto dto);
    Task DeleteAnnouncementAsync(Guid id);
    Task<IEnumerable<AnnouncementResponseDto>> GetAnnouncementsAsync(string? audience);
    Task<AnnouncementResponseDto> GetAnnouncementByIdAsync(Guid id);
}