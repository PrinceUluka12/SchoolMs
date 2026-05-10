using SchoolMS.Core.DTOs.Academic;

namespace SchoolMS.Core.Interfaces;

public interface IClassService
{
    Task<ClassResponseDto> CreateAsync(CreateClassDto dto);
    Task<ClassResponseDto> GetByIdAsync(Guid id);
    Task<IEnumerable<ClassResponseDto>> GetAllAsync(Guid? academicYearId);
    Task<ClassResponseDto> UpdateAsync(Guid id, UpdateClassDto dto);
    Task DeleteAsync(Guid id);
    Task<ClassResponseDto> AssignTeacherAsync(Guid classId, Guid teacherId);
    Task<ClassSubjectResponseDto> AssignSubjectAsync(Guid classId, AssignSubjectDto dto);
    Task RemoveSubjectAsync(Guid classId, Guid subjectId);
    Task<IEnumerable<ClassSubjectResponseDto>> GetSubjectsAsync(Guid classId);
}