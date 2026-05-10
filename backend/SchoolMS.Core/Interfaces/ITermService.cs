using SchoolMS.Core.DTOs.Academic;

namespace SchoolMS.Core.Interfaces;

public interface ITermService
{
    Task<TermResponseDto> CreateAsync(CreateTermDto dto);
    Task<TermResponseDto> GetByIdAsync(Guid id);
    Task<IEnumerable<TermResponseDto>> GetByAcademicYearAsync(Guid academicYearId);
    Task<TermResponseDto> UpdateAsync(Guid id, UpdateTermDto dto);
    Task DeleteAsync(Guid id);
    Task<TermResponseDto> SetCurrentAsync(Guid id);
    Task<TermResponseDto?> GetCurrentAsync();
}