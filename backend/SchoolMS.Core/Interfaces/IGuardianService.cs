using SchoolMS.Core.DTOs.Students;

namespace SchoolMS.Core.Interfaces;

public interface IGuardianService
{
    Task<GuardianResponseDto> CreateAsync(CreateGuardianDto dto);
    Task<GuardianResponseDto> GetByIdAsync(Guid id);
    Task<IEnumerable<GuardianResponseDto>> GetAllAsync();
    Task<GuardianResponseDto> UpdateAsync(Guid id, UpdateGuardianDto dto);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<StudentResponseDto>> GetStudentsByGuardianAsync(Guid guardianId);
}