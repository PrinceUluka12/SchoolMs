using SchoolMS.Core.DTOs.Academic;

namespace SchoolMS.Core.Interfaces;

public interface IAcademicYearService
{
    Task<AcademicYearResponseDto> CreateAsync(CreateAcademicYearDto dto);
    Task<AcademicYearResponseDto> GetByIdAsync(Guid id);
    Task<IEnumerable<AcademicYearResponseDto>> GetAllAsync();
    Task<AcademicYearResponseDto> UpdateAsync(Guid id, UpdateAcademicYearDto dto);
    Task DeleteAsync(Guid id);
    Task<AcademicYearResponseDto> SetCurrentAsync(Guid id);
}