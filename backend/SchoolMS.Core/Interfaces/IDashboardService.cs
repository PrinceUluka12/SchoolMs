using SchoolMS.Core.DTOs.Dashboard;

namespace SchoolMS.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
}