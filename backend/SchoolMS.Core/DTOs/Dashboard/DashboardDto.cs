namespace SchoolMS.Core.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }
    public int TotalStaff { get; set; }
    public int ActiveStaff { get; set; }
    public int TotalClasses { get; set; }
    public string? CurrentAcademicYear { get; set; }
    public string? CurrentTerm { get; set; }
    public List<CountByLabelDto> StudentsByGender { get; set; } = new();
    public List<CountByLabelDto> StudentsByClass { get; set; } = new();
    public List<CountByLabelDto> StaffByRole { get; set; } = new();
    public List<RecentStudentDto> RecentStudents { get; set; } = new();
}

public class CountByLabelDto
{
    public string Label { get; set; } = null!;
    public int Count { get; set; }
}

public class RecentStudentDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string? ClassName { get; set; }
    public DateTime CreatedAt { get; set; }
}