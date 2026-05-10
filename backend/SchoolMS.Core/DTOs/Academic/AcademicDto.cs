namespace SchoolMS.Core.DTOs.Academic;

// ── Department ────────────────────────────────────────────────────────────────
public class CreateDepartmentDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? HeadOfDepartmentId { get; set; }
}

public class UpdateDepartmentDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? HeadOfDepartmentId { get; set; }
}

public class DepartmentResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? HeadOfDepartmentId { get; set; }
    public string? HeadOfDepartmentName { get; set; }
    public int StaffCount { get; set; }
    public int SubjectCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Academic Year ─────────────────────────────────────────────────────────────
public class CreateAcademicYearDto
{
    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class UpdateAcademicYearDto
{
    public string? Name { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}

public class AcademicYearResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string Status { get; set; } = null!;
    public int TermCount { get; set; }
    public int ClassCount { get; set; }
    public List<TermResponseDto> Terms { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// ── Term ──────────────────────────────────────────────────────────────────────
public class CreateTermDto
{
    public string Name { get; set; } = null!;
    public int TermNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid AcademicYearId { get; set; }
}

public class UpdateTermDto
{
    public string? Name { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
}

public class TermResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int TermNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public string Status { get; set; } = null!;
    public Guid AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// ── Class ─────────────────────────────────────────────────────────────────────
public class CreateClassDto
{
    public string Name { get; set; } = null!;
    public string? Section { get; set; }
    public string? Stream { get; set; }
    public int Level { get; set; }
    public int Capacity { get; set; } = 40;
    public Guid AcademicYearId { get; set; }
    public Guid? ClassTeacherId { get; set; }
}

public class UpdateClassDto
{
    public string? Name { get; set; }
    public string? Section { get; set; }
    public string? Stream { get; set; }
    public int? Level { get; set; }
    public int? Capacity { get; set; }
    public Guid? ClassTeacherId { get; set; }
}

public class AssignSubjectDto
{
    public Guid SubjectId { get; set; }
    public Guid? TeacherId { get; set; }
}

public class ClassResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Section { get; set; }
    public string? Stream { get; set; }
    public int Level { get; set; }
    public int Capacity { get; set; }
    public string DisplayName => string.IsNullOrEmpty(Section) ? Name : $"{Name} {Section}";
    public Guid AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = null!;
    public Guid? ClassTeacherId { get; set; }
    public string? ClassTeacherName { get; set; }
    public int StudentCount { get; set; }
    public int SubjectCount { get; set; }
    public List<ClassSubjectResponseDto> Subjects { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ClassSubjectResponseDto
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public string SubjectName { get; set; } = null!;
    public string SubjectCode { get; set; } = null!;
    public Guid? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public bool IsActive { get; set; }
}

// ── Subject ───────────────────────────────────────────────────────────────────
public class CreateSubjectDto
{
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Type { get; set; } = "Core";
    public int CreditHours { get; set; } = 1;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
}

public class UpdateSubjectDto
{
    public string? Name { get; set; }
    public string? Code { get; set; }
    public string? Type { get; set; }
    public int? CreditHours { get; set; }
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool? IsActive { get; set; }
}

public class SubjectResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int CreditHours { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int ClassCount { get; set; }
    public DateTime CreatedAt { get; set; }
}