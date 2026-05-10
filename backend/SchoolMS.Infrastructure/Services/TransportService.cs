using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Transport;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;
using System.Text.Json;

namespace SchoolMS.Infrastructure.Services;

public class TransportService : ITransportService
{
    private readonly AppDbContext _db;

    public TransportService(AppDbContext db) { _db = db; }

    // ── Vehicles ──────────────────────────────────────────────────────────────
    public async Task<VehicleResponseDto> CreateVehicleAsync(CreateVehicleDto dto)
    {
        var exists = await _db.Vehicles.AnyAsync(v => v.PlateNumber == dto.PlateNumber);
        if (exists)
            throw new InvalidOperationException($"Vehicle with plate '{dto.PlateNumber}' already exists.");

        var vehicle = new Vehicle
        {
            PlateNumber = dto.PlateNumber.ToUpper().Trim(),
            Type = dto.Type,
            Capacity = dto.Capacity,
            Model = dto.Model,
            Color = dto.Color,
            DriverId = dto.DriverId,
            Status = "Active"
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();
        return await MapVehicleAsync(vehicle.Id);
    }

    public async Task<VehicleResponseDto> GetVehicleByIdAsync(Guid id)
    {
        var exists = await _db.Vehicles.AnyAsync(v => v.Id == id);
        if (!exists) throw new KeyNotFoundException($"Vehicle {id} not found.");
        return await MapVehicleAsync(id);
    }

    public async Task<IEnumerable<VehicleResponseDto>> GetVehiclesAsync()
    {
        var vehicles = await _db.Vehicles
            .Include(v => v.Driver)
            .Include(v => v.Routes)
            .OrderBy(v => v.PlateNumber)
            .ToListAsync();

        return vehicles.Select(v => new VehicleResponseDto
        {
            Id = v.Id,
            PlateNumber = v.PlateNumber,
            Type = v.Type,
            Capacity = v.Capacity,
            Model = v.Model,
            Color = v.Color,
            Status = v.Status,
            DriverId = v.DriverId,
            DriverName = v.Driver != null ? $"{v.Driver.FirstName} {v.Driver.LastName}" : null,
            RouteCount = v.Routes.Count,
            CreatedAt = v.CreatedAt
        });
    }

    public async Task<VehicleResponseDto> UpdateVehicleAsync(Guid id, UpdateVehicleDto dto)
    {
        var vehicle = await _db.Vehicles.FindAsync(id)
            ?? throw new KeyNotFoundException($"Vehicle {id} not found.");

        if (dto.Type != null) vehicle.Type = dto.Type;
        if (dto.Capacity.HasValue) vehicle.Capacity = dto.Capacity.Value;
        if (dto.Model != null) vehicle.Model = dto.Model;
        if (dto.Color != null) vehicle.Color = dto.Color;
        if (dto.DriverId.HasValue) vehicle.DriverId = dto.DriverId;
        if (dto.Status != null) vehicle.Status = dto.Status;

        await _db.SaveChangesAsync();
        return await MapVehicleAsync(id);
    }

    public async Task DeleteVehicleAsync(Guid id)
    {
        var vehicle = await _db.Vehicles
            .Include(v => v.Routes)
            .FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new KeyNotFoundException($"Vehicle {id} not found.");

        if (vehicle.Routes.Any(r => r.IsActive))
            throw new InvalidOperationException("Cannot delete a vehicle assigned to active routes.");

        vehicle.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Routes ────────────────────────────────────────────────────────────────
    public async Task<RouteResponseDto> CreateRouteAsync(CreateRouteDto dto)
    {
        var route = new TransportRoute
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            StopsJson = JsonSerializer.Serialize(dto.Stops),
            MorningPickupFee = dto.MorningPickupFee,
            AfternoonDropFee = dto.AfternoonDropFee,
            VehicleId = dto.VehicleId,
            IsActive = true
        };

        _db.TransportRoutes.Add(route);
        await _db.SaveChangesAsync();
        return await MapRouteAsync(route.Id);
    }

    public async Task<RouteResponseDto> GetRouteByIdAsync(Guid id)
    {
        var exists = await _db.TransportRoutes.AnyAsync(r => r.Id == id);
        if (!exists) throw new KeyNotFoundException($"Route {id} not found.");
        return await MapRouteAsync(id);
    }

    public async Task<IEnumerable<RouteResponseDto>> GetRoutesAsync()
    {
        var routes = await _db.TransportRoutes
            .Include(r => r.Vehicle).ThenInclude(v => v!.Driver)
            .Include(r => r.StudentSubscriptions)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return routes.Select(MapRouteToDto);
    }

    public async Task<RouteResponseDto> UpdateRouteAsync(Guid id, UpdateRouteDto dto)
    {
        var route = await _db.TransportRoutes.FindAsync(id)
            ?? throw new KeyNotFoundException($"Route {id} not found.");

        if (dto.Name != null) route.Name = dto.Name.Trim();
        if (dto.Description != null) route.Description = dto.Description;
        if (dto.Stops != null) route.StopsJson = JsonSerializer.Serialize(dto.Stops);
        if (dto.MorningPickupFee.HasValue) route.MorningPickupFee = dto.MorningPickupFee;
        if (dto.AfternoonDropFee.HasValue) route.AfternoonDropFee = dto.AfternoonDropFee;
        if (dto.VehicleId.HasValue) route.VehicleId = dto.VehicleId;
        if (dto.IsActive.HasValue) route.IsActive = dto.IsActive.Value;

        await _db.SaveChangesAsync();
        return await MapRouteAsync(id);
    }

    public async Task DeleteRouteAsync(Guid id)
    {
        var route = await _db.TransportRoutes
            .Include(r => r.StudentSubscriptions)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Route {id} not found.");

        if (route.StudentSubscriptions.Any(s => s.IsActive))
            throw new InvalidOperationException("Cannot delete a route with active student subscriptions.");

        route.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Student Transport ─────────────────────────────────────────────────────
    public async Task<StudentTransportResponseDto> AssignStudentAsync(AssignStudentTransportDto dto)
    {
        var studentExists = await _db.Students.AnyAsync(s => s.Id == dto.StudentId);
        if (!studentExists) throw new KeyNotFoundException($"Student {dto.StudentId} not found.");

        var routeExists = await _db.TransportRoutes.AnyAsync(r => r.Id == dto.RouteId);
        if (!routeExists) throw new KeyNotFoundException($"Route {dto.RouteId} not found.");

        var existing = await _db.StudentTransports.AnyAsync(st =>
            st.StudentId == dto.StudentId &&
            st.RouteId == dto.RouteId &&
            st.TermId == dto.TermId &&
            st.IsActive);

        if (existing)
            throw new InvalidOperationException("Student is already assigned to this route for this term.");

        var assignment = new StudentTransport
        {
            StudentId = dto.StudentId,
            RouteId = dto.RouteId,
            TermId = dto.TermId,
            PickupStop = dto.PickupStop,
            DropStop = dto.DropStop,
            Type = dto.Type,
            IsActive = true
        };

        _db.StudentTransports.Add(assignment);
        await _db.SaveChangesAsync();
        return await MapStudentTransportAsync(assignment.Id);
    }

    public async Task<IEnumerable<StudentTransportResponseDto>> GetStudentsByRouteAsync(
        Guid routeId, Guid termId)
    {
        var assignments = await _db.StudentTransports
            .Include(st => st.Student)
            .Include(st => st.Route)
            .Include(st => st.Term)
            .Where(st => st.RouteId == routeId && st.TermId == termId && st.IsActive)
            .OrderBy(st => st.Student.LastName)
            .ToListAsync();

        return assignments.Select(MapStudentTransportToDto);
    }

    public async Task RemoveStudentFromRouteAsync(Guid studentTransportId)
    {
        var assignment = await _db.StudentTransports.FindAsync(studentTransportId)
            ?? throw new KeyNotFoundException($"Transport assignment {studentTransportId} not found.");

        assignment.IsActive = false;
        await _db.SaveChangesAsync();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task<VehicleResponseDto> MapVehicleAsync(Guid id)
    {
        var v = await _db.Vehicles
            .Include(x => x.Driver)
            .Include(x => x.Routes)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();

        return new VehicleResponseDto
        {
            Id = v.Id,
            PlateNumber = v.PlateNumber,
            Type = v.Type,
            Capacity = v.Capacity,
            Model = v.Model,
            Color = v.Color,
            Status = v.Status,
            DriverId = v.DriverId,
            DriverName = v.Driver != null ? $"{v.Driver.FirstName} {v.Driver.LastName}" : null,
            RouteCount = v.Routes.Count,
            CreatedAt = v.CreatedAt
        };
    }

    private async Task<RouteResponseDto> MapRouteAsync(Guid id)
    {
        var r = await _db.TransportRoutes
            .Include(x => x.Vehicle).ThenInclude(v => v!.Driver)
            .Include(x => x.StudentSubscriptions)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapRouteToDto(r);
    }

    private static RouteResponseDto MapRouteToDto(TransportRoute r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        Stops = JsonSerializer.Deserialize<List<string>>(r.StopsJson) ?? new(),
        MorningPickupFee = r.MorningPickupFee,
        AfternoonDropFee = r.AfternoonDropFee,
        IsActive = r.IsActive,
        VehicleId = r.VehicleId,
        VehiclePlate = r.Vehicle?.PlateNumber,
        DriverName = r.Vehicle?.Driver != null
            ? $"{r.Vehicle.Driver.FirstName} {r.Vehicle.Driver.LastName}"
            : null,
        StudentCount = r.StudentSubscriptions.Count(s => s.IsActive),
        CreatedAt = r.CreatedAt
    };

    private async Task<StudentTransportResponseDto> MapStudentTransportAsync(Guid id)
    {
        var st = await _db.StudentTransports
            .Include(x => x.Student)
            .Include(x => x.Route)
            .Include(x => x.Term)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapStudentTransportToDto(st);
    }

    private static StudentTransportResponseDto MapStudentTransportToDto(StudentTransport st) => new()
    {
        Id = st.Id,
        StudentId = st.StudentId,
        StudentName = $"{st.Student.FirstName} {st.Student.LastName}",
        StudentNumber = st.Student.StudentNumber,
        RouteId = st.RouteId,
        RouteName = st.Route.Name,
        PickupStop = st.PickupStop,
        DropStop = st.DropStop,
        Type = st.Type,
        IsActive = st.IsActive,
        TermName = st.Term.Name,
        CreatedAt = st.CreatedAt
    };
}