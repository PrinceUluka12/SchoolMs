using SchoolMS.Core.DTOs.Timetable;

namespace SchoolMS.Core.Interfaces;

public interface ITimetableService
{
    Task<TimetableSlotResponseDto> CreateSlotAsync(CreateTimetableSlotDto dto);
    Task<TimetableSlotResponseDto> UpdateSlotAsync(Guid id, UpdateTimetableSlotDto dto);
    Task DeleteSlotAsync(Guid id);
    Task<ClassTimetableDto> GetClassTimetableAsync(Guid classId, Guid academicYearId);
    Task<TeacherTimetableDto> GetTeacherTimetableAsync(Guid teacherId, Guid academicYearId);
    Task<ConflictCheckDto> CheckConflictAsync(CreateTimetableSlotDto dto, Guid? excludeSlotId = null);
    Task<List<TimetableSlotResponseDto>> GetAllSlotsAsync(Guid academicYearId, Guid? classId);
}