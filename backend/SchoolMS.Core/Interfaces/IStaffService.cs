using Microsoft.AspNetCore.Http;
using SchoolMS.Core.DTOs.Common;
using SchoolMS.Core.DTOs.Staff;

namespace SchoolMS.Core.Interfaces;

public interface IStaffService
{
    Task<StaffResponseDto> CreateAsync(CreateStaffDto dto);
    Task<StaffResponseDto> GetByIdAsync(Guid id);
    Task<PagedResult<StaffResponseDto>> GetAllAsync(StaffFilterDto filter);
    Task<StaffResponseDto> UpdateAsync(Guid id, UpdateStaffDto dto);
    Task DeleteAsync(Guid id);
    Task<StaffResponseDto> UpdateStatusAsync(Guid id, string status);
    Task<string> UploadPhotoAsync(Guid id, IFormFile file);
    Task<StaffDocumentResponseDto> UploadDocumentAsync(Guid id, UploadStaffDocumentDto dto);
    Task DeleteDocumentAsync(Guid documentId);
    Task<IEnumerable<StaffDocumentResponseDto>> GetDocumentsAsync(Guid staffId);
}