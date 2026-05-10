namespace SchoolMS.Core.DTOs.Common;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public class StudentFilterDto
{
    public string? Search { get; set; }        // name or student number
    public Guid? ClassId { get; set; }
    public string? Status { get; set; }
    public string? Gender { get; set; }
    public Guid? AcademicYearId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "LastName";
    public string SortDirection { get; set; } = "asc";
}