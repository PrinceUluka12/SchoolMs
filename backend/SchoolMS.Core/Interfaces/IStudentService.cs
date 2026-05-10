using Microsoft.AspNetCore.Http;
using SchoolMS.Core.DTOs.Common;
using SchoolMS.Core.DTOs.Students;

namespace SchoolMS.Core.Interfaces;

public interface IStudentService
{
    Task<StudentResponseDto> CreateAsync(CreateStudentDto dto);
    Task<StudentResponseDto> GetByIdAsync(Guid id);
    Task<PagedResult<StudentResponseDto>> GetAllAsync(StudentFilterDto filter);
    Task<StudentResponseDto> UpdateAsync(Guid id, UpdateStudentDto dto);
    Task DeleteAsync(Guid id);
    Task<StudentResponseDto> AssignClassAsync(Guid studentId, Guid classId);
    Task<StudentResponseDto> TransferAsync(Guid studentId, TransferStudentDto dto);
    Task<StudentResponseDto> UpdateStatusAsync(Guid studentId, string status);
    Task<string> UploadPhotoAsync(Guid studentId, IFormFile file);
    Task<StudentDocumentResponseDto> UploadDocumentAsync(Guid studentId, UploadDocumentDto dto);
    Task DeleteDocumentAsync(Guid documentId);
    Task<IEnumerable<StudentDocumentResponseDto>> GetDocumentsAsync(Guid studentId);
    Task<BulkImportResultDto> BulkImportAsync(IFormFile csvFile);
    Task<byte[]> GenerateStudentIdCardAsync(Guid studentId);
}