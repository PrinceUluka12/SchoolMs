using SchoolMS.Core.DTOs.Hostel;

namespace SchoolMS.Core.Interfaces;

public interface IHostelService
{
    Task<BuildingResponseDto> CreateBuildingAsync(CreateBuildingDto dto);
    Task<BuildingResponseDto> GetBuildingByIdAsync(Guid id);
    Task<IEnumerable<BuildingResponseDto>> GetBuildingsAsync();
    Task<BuildingResponseDto> UpdateBuildingAsync(Guid id, UpdateBuildingDto dto);
    Task DeleteBuildingAsync(Guid id);
    Task<RoomResponseDto> CreateRoomAsync(CreateRoomDto dto);
    Task<RoomResponseDto> GetRoomByIdAsync(Guid id);
    Task<IEnumerable<RoomResponseDto>> GetRoomsByBuildingAsync(Guid buildingId);
    Task<RoomResponseDto> UpdateRoomAsync(Guid id, UpdateRoomDto dto);
    Task DeleteRoomAsync(Guid id);
    Task<AllocationResponseDto> AllocateStudentAsync(AllocateStudentDto dto);
    Task DeallocateStudentAsync(Guid allocationId);
    Task<IEnumerable<AllocationResponseDto>> GetAllocationsByTermAsync(Guid termId);
    Task<AllocationResponseDto?> GetStudentAllocationAsync(Guid studentId, Guid termId);
}