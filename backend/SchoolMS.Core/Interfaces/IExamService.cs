using SchoolMS.Core.DTOs.Exams;

namespace SchoolMS.Core.Interfaces;

public interface IExamService
{
    Task<ExamResponseDto> CreateExamAsync(CreateExamDto dto);
    Task<ExamResponseDto> GetExamByIdAsync(Guid id);
    Task<IEnumerable<ExamResponseDto>> GetExamsByTermAsync(Guid termId);
    Task<ExamResponseDto> UpdateExamAsync(Guid id, UpdateExamDto dto);
    Task DeleteExamAsync(Guid id);
    Task<ExamScheduleResponseDto> AddScheduleAsync(CreateExamScheduleDto dto);
    Task DeleteScheduleAsync(Guid scheduleId);
    Task<IEnumerable<ExamScheduleResponseDto>> GetSchedulesByExamAsync(Guid examId);
    Task<List<SeatingArrangementResponseDto>> GenerateSeatingAsync(Guid examScheduleId);
    Task<IEnumerable<SeatingArrangementResponseDto>> GetSeatingAsync(Guid examScheduleId);
    Task<byte[]> GenerateAdmitCardPdfAsync(Guid studentId, Guid examId);
    Task<byte[]> GenerateSeatingPdfAsync(Guid examScheduleId);
}