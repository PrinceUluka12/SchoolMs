using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Hostel;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class HostelService : IHostelService
{
    private readonly AppDbContext _db;

    public HostelService(AppDbContext db) { _db = db; }

    // ── Buildings ─────────────────────────────────────────────────────────────
    public async Task<BuildingResponseDto> CreateBuildingAsync(CreateBuildingDto dto)
    {
        var building = new HostelBuilding
        {
            Name = dto.Name.Trim(),
            Gender = dto.Gender,
            Description = dto.Description,
            WardenId = dto.WardenId
        };

        _db.HostelBuildings.Add(building);
        await _db.SaveChangesAsync();
        return await MapBuildingAsync(building.Id);
    }

    public async Task<BuildingResponseDto> GetBuildingByIdAsync(Guid id)
    {
        var exists = await _db.HostelBuildings.AnyAsync(b => b.Id == id);
        if (!exists) throw new KeyNotFoundException($"Building {id} not found.");
        return await MapBuildingAsync(id);
    }

    public async Task<IEnumerable<BuildingResponseDto>> GetBuildingsAsync()
    {
        var buildings = await _db.HostelBuildings
            .Include(b => b.Warden)
            .Include(b => b.Rooms).ThenInclude(r => r.Allocations.Where(a => a.IsActive))
            .OrderBy(b => b.Name)
            .ToListAsync();

        return buildings.Select(MapBuildingToDto);
    }

    public async Task<BuildingResponseDto> UpdateBuildingAsync(Guid id, UpdateBuildingDto dto)
    {
        var building = await _db.HostelBuildings.FindAsync(id)
            ?? throw new KeyNotFoundException($"Building {id} not found.");

        if (dto.Name != null) building.Name = dto.Name.Trim();
        if (dto.Gender != null) building.Gender = dto.Gender;
        if (dto.Description != null) building.Description = dto.Description;
        if (dto.WardenId.HasValue) building.WardenId = dto.WardenId;

        await _db.SaveChangesAsync();
        return await MapBuildingAsync(id);
    }

    public async Task DeleteBuildingAsync(Guid id)
    {
        var building = await _db.HostelBuildings
            .Include(b => b.Rooms).ThenInclude(r => r.Allocations)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new KeyNotFoundException($"Building {id} not found.");

        if (building.Rooms.Any(r => r.Allocations.Any(a => a.IsActive)))
            throw new InvalidOperationException("Cannot delete a building with active student allocations.");

        building.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Rooms ─────────────────────────────────────────────────────────────────
    public async Task<RoomResponseDto> CreateRoomAsync(CreateRoomDto dto)
    {
        var buildingExists = await _db.HostelBuildings.AnyAsync(b => b.Id == dto.BuildingId);
        if (!buildingExists) throw new KeyNotFoundException($"Building {dto.BuildingId} not found.");

        var room = new HostelRoom
        {
            RoomNumber = dto.RoomNumber.Trim(),
            Capacity = dto.Capacity,
            Type = dto.Type,
            BuildingId = dto.BuildingId,
            Status = "Available"
        };

        _db.HostelRooms.Add(room);
        await _db.SaveChangesAsync();
        return await MapRoomAsync(room.Id);
    }

    public async Task<RoomResponseDto> GetRoomByIdAsync(Guid id)
    {
        var exists = await _db.HostelRooms.AnyAsync(r => r.Id == id);
        if (!exists) throw new KeyNotFoundException($"Room {id} not found.");
        return await MapRoomAsync(id);
    }

    public async Task<IEnumerable<RoomResponseDto>> GetRoomsByBuildingAsync(Guid buildingId)
    {
        var rooms = await _db.HostelRooms
            .Include(r => r.Building)
            .Include(r => r.Allocations.Where(a => a.IsActive)).ThenInclude(a => a.Student)
            .Where(r => r.BuildingId == buildingId)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

        return rooms.Select(MapRoomToDto);
    }

    public async Task<RoomResponseDto> UpdateRoomAsync(Guid id, UpdateRoomDto dto)
    {
        var room = await _db.HostelRooms.FindAsync(id)
            ?? throw new KeyNotFoundException($"Room {id} not found.");

        if (dto.RoomNumber != null) room.RoomNumber = dto.RoomNumber.Trim();
        if (dto.Capacity.HasValue) room.Capacity = dto.Capacity.Value;
        if (dto.Type != null) room.Type = dto.Type;
        if (dto.Status != null) room.Status = dto.Status;

        await _db.SaveChangesAsync();
        return await MapRoomAsync(id);
    }

    public async Task DeleteRoomAsync(Guid id)
    {
        var room = await _db.HostelRooms
            .Include(r => r.Allocations)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Room {id} not found.");

        if (room.Allocations.Any(a => a.IsActive))
            throw new InvalidOperationException("Cannot delete a room with active allocations.");

        room.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Allocations ───────────────────────────────────────────────────────────
    public async Task<AllocationResponseDto> AllocateStudentAsync(AllocateStudentDto dto)
    {
        // Check student not already allocated this term
        var alreadyAllocated = await _db.HostelAllocations.AnyAsync(a =>
            a.StudentId == dto.StudentId && a.TermId == dto.TermId && a.IsActive);

        if (alreadyAllocated)
            throw new InvalidOperationException("Student already has a hostel allocation for this term.");

        var room = await _db.HostelRooms
            .Include(r => r.Allocations.Where(a => a.IsActive))
            .FirstOrDefaultAsync(r => r.Id == dto.RoomId)
            ?? throw new KeyNotFoundException($"Room {dto.RoomId} not found.");

        if (room.Allocations.Count >= room.Capacity)
            throw new InvalidOperationException("Room is at full capacity.");

        // Check bed number not taken
        var bedTaken = room.Allocations.Any(a => a.BedNumber == dto.BedNumber);
        if (bedTaken)
            throw new InvalidOperationException($"Bed {dto.BedNumber} is already occupied.");

        var allocation = new HostelAllocation
        {
            StudentId = dto.StudentId,
            RoomId = dto.RoomId,
            TermId = dto.TermId,
            BedNumber = dto.BedNumber,
            IsActive = true
        };

        _db.HostelAllocations.Add(allocation);

        // Update room status
        if (room.Allocations.Count + 1 >= room.Capacity)
            room.Status = "Full";

        await _db.SaveChangesAsync();
        return await MapAllocationAsync(allocation.Id);
    }

    public async Task DeallocateStudentAsync(Guid allocationId)
    {
        var allocation = await _db.HostelAllocations
            .Include(a => a.Room)
            .FirstOrDefaultAsync(a => a.Id == allocationId)
            ?? throw new KeyNotFoundException($"Allocation {allocationId} not found.");

        allocation.IsActive = false;
        allocation.Room.Status = "Available";
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<AllocationResponseDto>> GetAllocationsByTermAsync(Guid termId)
    {
        var allocations = await _db.HostelAllocations
            .Include(a => a.Student)
            .Include(a => a.Room).ThenInclude(r => r.Building)
            .Include(a => a.Term)
            .Where(a => a.TermId == termId && a.IsActive)
            .OrderBy(a => a.Room.Building.Name)
            .ThenBy(a => a.Room.RoomNumber)
            .ToListAsync();

        return allocations.Select(MapAllocationToDto);
    }

    public async Task<AllocationResponseDto?> GetStudentAllocationAsync(Guid studentId, Guid termId)
    {
        var allocation = await _db.HostelAllocations
            .Include(a => a.Student)
            .Include(a => a.Room).ThenInclude(r => r.Building)
            .Include(a => a.Term)
            .FirstOrDefaultAsync(a => a.StudentId == studentId && a.TermId == termId && a.IsActive);

        return allocation == null ? null : MapAllocationToDto(allocation);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task<BuildingResponseDto> MapBuildingAsync(Guid id)
    {
        var b = await _db.HostelBuildings
            .Include(x => x.Warden)
            .Include(x => x.Rooms).ThenInclude(r => r.Allocations.Where(a => a.IsActive)).ThenInclude(a => a.Student)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapBuildingToDto(b);
    }

    private static BuildingResponseDto MapBuildingToDto(HostelBuilding b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Gender = b.Gender,
        Description = b.Description,
        WardenId = b.WardenId,
        WardenName = b.Warden != null ? $"{b.Warden.FirstName} {b.Warden.LastName}" : null,
        TotalRooms = b.Rooms.Count,
        TotalCapacity = b.Rooms.Sum(r => r.Capacity),
        OccupiedBeds = b.Rooms.Sum(r => r.Allocations.Count(a => a.IsActive)),
        Rooms = b.Rooms.Select(r => new RoomResponseDto
        {
            Id = r.Id,
            RoomNumber = r.RoomNumber,
            Capacity = r.Capacity,
            Type = r.Type,
            Status = r.Status,
            BuildingId = r.BuildingId,
            BuildingName = b.Name,
            OccupiedBeds = r.Allocations.Count(a => a.IsActive),
            CurrentAllocations = r.Allocations.Where(a => a.IsActive).Select(a => new AllocationResponseDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
                StudentNumber = a.Student.StudentNumber,
                RoomId = r.Id,
                RoomNumber = r.RoomNumber,
                BuildingName = b.Name,
                BedNumber = a.BedNumber,
                IsActive = a.IsActive,
                TermName = "",
                CreatedAt = a.CreatedAt
            }).ToList(),
            CreatedAt = r.CreatedAt
        }).ToList(),
        CreatedAt = b.CreatedAt
    };

    private async Task<RoomResponseDto> MapRoomAsync(Guid id)
    {
        var r = await _db.HostelRooms
            .Include(x => x.Building)
            .Include(x => x.Allocations.Where(a => a.IsActive)).ThenInclude(a => a.Student)
            .Include(x => x.Allocations.Where(a => a.IsActive)).ThenInclude(a => a.Term)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapRoomToDto(r);
    }

    private static RoomResponseDto MapRoomToDto(HostelRoom r) => new()
    {
        Id = r.Id,
        RoomNumber = r.RoomNumber,
        Capacity = r.Capacity,
        Type = r.Type,
        Status = r.Status,
        BuildingId = r.BuildingId,
        BuildingName = r.Building?.Name ?? "",
        OccupiedBeds = r.Allocations.Count(a => a.IsActive),
        CurrentAllocations = r.Allocations.Where(a => a.IsActive).Select(a => new AllocationResponseDto
        {
            Id = a.Id,
            StudentId = a.StudentId,
            StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
            StudentNumber = a.Student.StudentNumber,
            RoomId = r.Id,
            RoomNumber = r.RoomNumber,
            BuildingName = r.Building?.Name ?? "",
            BedNumber = a.BedNumber,
            IsActive = a.IsActive,
            TermName = a.Term?.Name ?? "",
            CreatedAt = a.CreatedAt
        }).ToList(),
        CreatedAt = r.CreatedAt
    };

    private async Task<AllocationResponseDto> MapAllocationAsync(Guid id)
    {
        var a = await _db.HostelAllocations
            .Include(x => x.Student)
            .Include(x => x.Room).ThenInclude(r => r.Building)
            .Include(x => x.Term)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapAllocationToDto(a);
    }

    private static AllocationResponseDto MapAllocationToDto(HostelAllocation a) => new()
    {
        Id = a.Id,
        StudentId = a.StudentId,
        StudentName = $"{a.Student.FirstName} {a.Student.LastName}",
        StudentNumber = a.Student.StudentNumber,
        RoomId = a.RoomId,
        RoomNumber = a.Room.RoomNumber,
        BuildingName = a.Room.Building?.Name ?? "",
        BedNumber = a.BedNumber,
        IsActive = a.IsActive,
        TermName = a.Term?.Name ?? "",
        CreatedAt = a.CreatedAt
    };
}