using SchoolMS.Core.DTOs.Grades;

namespace SchoolMS.Core.Interfaces;

public interface IGradeService
{
    Task<GradeResponseDto> EnterGradeAsync(EnterGradeDto dto, Guid enteredById);
    Task<List<GradeResponseDto>> BulkEnterGradesAsync(BulkEnterGradesDto dto, Guid enteredById);
    Task<GradeResponseDto> UpdateGradeAsync(Guid gradeId, decimal score, string? remark, Guid updatedById);
    Task DeleteGradeAsync(Guid gradeId);
    Task LockGradesAsync(Guid classId, Guid termId);
    Task UnlockGradesAsync(Guid classId, Guid termId);
    Task<ClassGradeSheetDto> GetClassGradeSheetAsync(Guid classId, Guid termId, Guid subjectId, string assessmentType);
    Task<StudentTermReportDto> GetStudentTermReportAsync(Guid studentId, Guid termId);
    Task<List<StudentTermReportDto>> GetClassTermReportsAsync(Guid classId, Guid termId);
    Task SetAssessmentWeightsAsync(SetAssessmentWeightDto dto);
    Task<List<AssessmentWeightEntryDto>> GetAssessmentWeightsAsync(Guid classId, Guid termId);
    Task<byte[]> GenerateReportCardPdfAsync(Guid studentId, Guid termId);
    Task<byte[]> GenerateClassReportCardsPdfAsync(Guid classId, Guid termId);
}