namespace SchoolMS.Core.DTOs.Hostel;

// ── Building ──────────────────────────────────────────────────────────────────
public class CreateBuildingDto
{
    public string Name { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? WardenId { get; set; }
}

public class UpdateBuildingDto
{
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public string? Description { get; set; }
    public Guid? WardenId { get; set; }
}

public class BuildingResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Gender { get; set; } = null!;
    public string? Description { get; set; }
    public Guid? WardenId { get; set; }
    public string? WardenName { get; set; }
    public int TotalRooms { get; set; }
    public int TotalCapacity { get; set; }
    public int OccupiedBeds { get; set; }
    public int AvailableBeds => TotalCapacity - OccupiedBeds;
    public List<RoomResponseDto> Rooms { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// ── Room ──────────────────────────────────────────────────────────────────────
public class CreateRoomDto
{
    public string RoomNumber { get; set; } = null!;
    public int Capacity { get; set; }
    public string Type { get; set; } = "Dormitory";
    public Guid BuildingId { get; set; }
}

public class UpdateRoomDto
{
    public string? RoomNumber { get; set; }
    public int? Capacity { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
}

public class RoomResponseDto
{
    public Guid Id { get; set; }
    public string RoomNumber { get; set; } = null!;
    public int Capacity { get; set; }
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid BuildingId { get; set; }
    public string BuildingName { get; set; } = null!;
    public int OccupiedBeds { get; set; }
    public int AvailableBeds => Capacity - OccupiedBeds;
    public List<AllocationResponseDto> CurrentAllocations { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

// ── Allocation ────────────────────────────────────────────────────────────────
public class AllocateStudentDto
{
    public Guid StudentId { get; set; }
    public Guid RoomId { get; set; }
    public Guid TermId { get; set; }
    public string BedNumber { get; set; } = null!;
}

public class AllocationResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = null!;
    public string BuildingName { get; set; } = null!;
    public string BedNumber { get; set; } = null!;
    public bool IsActive { get; set; }
    public string TermName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}