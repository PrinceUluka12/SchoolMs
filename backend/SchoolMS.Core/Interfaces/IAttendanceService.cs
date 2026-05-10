using SchoolMS.Core.DTOs.Attendance;

namespace SchoolMS.Core.Interfaces;

public interface IAttendanceService
{
    Task<List<AttendanceResponseDto>> MarkBulkAsync(MarkAttendanceDto dto, Guid markedById);
    Task<AttendanceResponseDto> UpdateAsync(Guid attendanceId, UpdateAttendanceDto dto);
    Task<AttendanceRegisterDto> GetRegisterAsync(Guid classId, DateTime date, int? period);
    Task<List<AttendanceSummaryDto>> GetStudentSummaryAsync(Guid studentId, Guid? termId, DateTime? from, DateTime? to);
    Task<ClassAttendanceReportDto> GetClassReportAsync(Guid classId, DateTime from, DateTime to);
    Task<List<AttendanceSummaryDto>> GetLowAttendanceAsync(Guid classId, Guid termId, decimal threshold);
    Task<Guid> GetStaffByUserIdAsync(Guid userId);
}