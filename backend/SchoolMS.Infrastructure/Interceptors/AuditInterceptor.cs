using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using SchoolMS.Core.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace SchoolMS.Infrastructure.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AuditEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AuditEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AuditEntities(DbContext? context)
    {
        if (context == null) return;

        var httpContext = _httpContextAccessor.HttpContext;
        var actorIdStr = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var actorEmail = httpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? "system";
        var ip = httpContext?.Connection?.RemoteIpAddress?.ToString();
        Guid.TryParse(actorIdStr, out var actorId);

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var logs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            // Update timestamps on BaseEntity
            if (entry.Entity is BaseEntity baseEntity)
            {
                if (entry.State == EntityState.Added)
                    baseEntity.CreatedAt = baseEntity.UpdatedAt = DateTime.UtcNow;
                else if (entry.State == EntityState.Modified)
                    baseEntity.UpdatedAt = DateTime.UtcNow;
            }

            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => "Unknown"
            };

            var oldValues = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                ? JsonSerializer.Serialize(entry.OriginalValues.ToObject())
                : null;

            var newValues = entry.State == EntityState.Added || entry.State == EntityState.Modified
                ? JsonSerializer.Serialize(entry.CurrentValues.ToObject())
                : null;

            logs.Add(new AuditLog
            {
                ActorId = actorId == Guid.Empty ? null : actorId,
                ActorEmail = actorEmail,
                Action = action,
                EntityType = entry.Entity.GetType().Name,
                EntityId = entry.Property("Id").CurrentValue?.ToString() ?? "",
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ip,
                Timestamp = DateTime.UtcNow
            });
        }

        context.Set<AuditLog>().AddRange(logs);
    }
}