using SchoolMS.Core.DTOs.Transport;

namespace SchoolMS.Core.Interfaces;

public interface ITransportService
{
    Task<VehicleResponseDto> CreateVehicleAsync(CreateVehicleDto dto);
    Task<VehicleResponseDto> GetVehicleByIdAsync(Guid id);
    Task<IEnumerable<VehicleResponseDto>> GetVehiclesAsync();
    Task<VehicleResponseDto> UpdateVehicleAsync(Guid id, UpdateVehicleDto dto);
    Task DeleteVehicleAsync(Guid id);
    Task<RouteResponseDto> CreateRouteAsync(CreateRouteDto dto);
    Task<RouteResponseDto> GetRouteByIdAsync(Guid id);
    Task<IEnumerable<RouteResponseDto>> GetRoutesAsync();
    Task<RouteResponseDto> UpdateRouteAsync(Guid id, UpdateRouteDto dto);
    Task DeleteRouteAsync(Guid id);
    Task<StudentTransportResponseDto> AssignStudentAsync(AssignStudentTransportDto dto);
    Task<IEnumerable<StudentTransportResponseDto>> GetStudentsByRouteAsync(Guid routeId, Guid termId);
    Task RemoveStudentFromRouteAsync(Guid studentTransportId);
}