using SchoolMS.Core.DTOs.Academic;

namespace SchoolMS.Core.Interfaces;

public interface IDepartmentService
{
    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentDto dto);
    Task<DepartmentResponseDto> GetByIdAsync(Guid id);
    Task<IEnumerable<DepartmentResponseDto>> GetAllAsync();
    Task<DepartmentResponseDto> UpdateAsync(Guid id, UpdateDepartmentDto dto);
    Task DeleteAsync(Guid id);
}