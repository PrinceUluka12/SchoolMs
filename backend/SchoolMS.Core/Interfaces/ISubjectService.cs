using SchoolMS.Core.DTOs.Academic;

namespace SchoolMS.Core.Interfaces;

public interface ISubjectService
{
    Task<SubjectResponseDto> CreateAsync(CreateSubjectDto dto);
    Task<SubjectResponseDto> GetByIdAsync(Guid id);
    Task<IEnumerable<SubjectResponseDto>> GetAllAsync();
    Task<SubjectResponseDto> UpdateAsync(Guid id, UpdateSubjectDto dto);
    Task DeleteAsync(Guid id);
}