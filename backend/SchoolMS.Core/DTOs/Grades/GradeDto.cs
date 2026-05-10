namespace SchoolMS.Core.DTOs.Grades;

// ── Requests ──────────────────────────────────────────────────────────────────
public class EnterGradeDto
{
    public Guid StudentId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid TermId { get; set; }
    public Guid ClassId { get; set; }
    public string AssessmentType { get; set; } = null!;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public string? TeacherRemark { get; set; }
}

public class BulkEnterGradesDto
{
    public Guid SubjectId { get; set; }
    public Guid TermId { get; set; }
    public Guid ClassId { get; set; }
    public string AssessmentType { get; set; } = null!;
    public decimal MaxScore { get; set; }
    public List<StudentGradeEntryDto> Entries { get; set; } = new();
}

public class StudentGradeEntryDto
{
    public Guid StudentId { get; set; }
    public decimal Score { get; set; }
    public string? TeacherRemark { get; set; }
}

public class SetAssessmentWeightDto
{
    public Guid ClassId { get; set; }
    public Guid TermId { get; set; }
    public List<AssessmentWeightEntryDto> Weights { get; set; } = new();
}

public class AssessmentWeightEntryDto
{
    public string AssessmentType { get; set; } = null!;
    public decimal Weight { get; set; }
}

// ── Responses ─────────────────────────────────────────────────────────────────
public class GradeResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = null!;
    public Guid TermId { get; set; }
    public string TermName { get; set; } = null!;
    public string AssessmentType { get; set; } = null!;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage => MaxScore > 0 ? Math.Round(Score / MaxScore * 100, 1) : 0;
    public bool IsLocked { get; set; }
    public string? TeacherRemark { get; set; }
    public string EnteredByName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class StudentTermReportDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string ClassName { get; set; } = null!;
    public string TermName { get; set; } = null!;
    public string AcademicYear { get; set; } = null!;
    public decimal OverallAverage { get; set; }
    public string OverallGrade { get; set; } = null!;
    public string OverallRemark { get; set; } = null!;
    public int ClassRank { get; set; }
    public int TotalStudents { get; set; }
    public decimal AttendanceRate { get; set; }
    public List<SubjectReportDto> Subjects { get; set; } = new();
}

public class SubjectReportDto
{
    public string SubjectName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public List<AssessmentScoreDto> Assessments { get; set; } = new();
    public decimal WeightedTotal { get; set; }
    public string Grade { get; set; } = null!;
    public string Remark { get; set; } = null!;
    public string? TeacherRemark { get; set; }
}

public class AssessmentScoreDto
{
    public string AssessmentType { get; set; } = null!;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public decimal Percentage => MaxScore > 0 ? Math.Round(Score / MaxScore * 100, 1) : 0;
}

public class ClassGradeSheetDto
{
    public string ClassName { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public string TermName { get; set; } = null!;
    public string AssessmentType { get; set; } = null!;
    public List<GradeResponseDto> Grades { get; set; } = new();
}