namespace SchoolMS.Core.DTOs.Transport;

// ── Vehicle ───────────────────────────────────────────────────────────────────
public class CreateVehicleDto
{
    public string PlateNumber { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int Capacity { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public Guid? DriverId { get; set; }
}

public class UpdateVehicleDto
{
    public string? Type { get; set; }
    public int? Capacity { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public Guid? DriverId { get; set; }
    public string? Status { get; set; }
}

public class VehicleResponseDto
{
    public Guid Id { get; set; }
    public string PlateNumber { get; set; } = null!;
    public string Type { get; set; } = null!;
    public int Capacity { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public string Status { get; set; } = null!;
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public int RouteCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Route ─────────────────────────────────────────────────────────────────────
public class CreateRouteDto
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public List<string> Stops { get; set; } = new();
    public decimal? MorningPickupFee { get; set; }
    public decimal? AfternoonDropFee { get; set; }
    public Guid? VehicleId { get; set; }
}

public class UpdateRouteDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? Stops { get; set; }
    public decimal? MorningPickupFee { get; set; }
    public decimal? AfternoonDropFee { get; set; }
    public Guid? VehicleId { get; set; }
    public bool? IsActive { get; set; }
}

public class RouteResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public List<string> Stops { get; set; } = new();
    public decimal? MorningPickupFee { get; set; }
    public decimal? AfternoonDropFee { get; set; }
    public bool IsActive { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehiclePlate { get; set; }
    public string? DriverName { get; set; }
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── Student Transport ─────────────────────────────────────────────────────────
public class AssignStudentTransportDto
{
    public Guid StudentId { get; set; }
    public Guid RouteId { get; set; }
    public Guid TermId { get; set; }
    public string PickupStop { get; set; } = null!;
    public string DropStop { get; set; } = null!;
    public string Type { get; set; } = "Both";
}

public class StudentTransportResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public Guid RouteId { get; set; }
    public string RouteName { get; set; } = null!;
    public string PickupStop { get; set; } = null!;
    public string DropStop { get; set; } = null!;
    public string Type { get; set; } = null!;
    public bool IsActive { get; set; }
    public string TermName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}