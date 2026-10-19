using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class AppointmentService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AppointmentService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<Appointment>> GetPagedAsync(
        int page = 1, int pageSize = 20,
        DateTime? from = null, DateTime? to = null,
        AppointmentStatus? status = null,
        int? mechanicId = null,
        int? clientId = null,
        string sortBy = "date", bool desc = false,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var q = db.Appointments.AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Car).ThenInclude(c => c.Brand)
            .Include(a => a.Service)
            .Include(a => a.Mechanic).ThenInclude(m => m!.User)
            .Include(a => a.RepairPlace)
            .AsQueryable();

        if (from.HasValue) q = q.Where(a => a.ScheduledAt >= from.Value);
        if (to.HasValue) q = q.Where(a => a.ScheduledAt <= to.Value);
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        if (mechanicId.HasValue) q = q.Where(a => a.MechanicId == mechanicId.Value);
        if (clientId.HasValue) q = q.Where(a => a.ClientId == clientId.Value);

        q = sortBy switch
        {
            "status" => desc ? q.OrderByDescending(a => a.Status) : q.OrderBy(a => a.Status),
            "client" => desc ? q.OrderByDescending(a => a.Client.FullName) : q.OrderBy(a => a.Client.FullName),
            _ => desc ? q.OrderByDescending(a => a.ScheduledAt) : q.OrderBy(a => a.ScheduledAt),
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Appointment>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Appointment?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Car).ThenInclude(c => c.Brand)
            .Include(a => a.Service)
            .Include(a => a.Mechanic).ThenInclude(m => m!.User)
            .Include(a => a.RepairPlace)
            .FirstOrDefaultAsync(a => a.AppointmentId == id, ct);
    }

    public async Task<Appointment> CreateAsync(Appointment a, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        a.CreatedAt = now;
        a.UpdatedAt = now;
        db.Appointments.Add(a);
        await db.SaveChangesAsync(ct);
        return a;
    }

    public async Task<Appointment> UpdateAsync(Appointment a, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.Appointments
            .FirstOrDefaultAsync(x => x.AppointmentId == a.AppointmentId, ct);
        if (existing is null) return a;

        existing.ClientId = a.ClientId;
        existing.CarId = a.CarId;
        existing.ServiceId = a.ServiceId;
        existing.MechanicId = a.MechanicId;
        existing.RepairPlaceId = a.RepairPlaceId;
        existing.ScheduledAt = a.ScheduledAt;
        existing.Status = a.Status;
        existing.Comment = a.Comment;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> ChangeStatusAsync(int id, AppointmentStatus status,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var a = await db.Appointments.FindAsync(new object?[] { id }, ct);
        if (a is null) return false;
        a.Status = status;
        a.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var a = await db.Appointments.FindAsync(new object?[] { id }, ct);
        if (a is null) return false;
        db.Appointments.Remove(a);
        await db.SaveChangesAsync(ct);
        return true;
    }
    public async Task<List<Service>> GetServicesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Services
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<Mechanic>> GetMechanicsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Mechanics
            .Include(m => m.User)
            .Where(m => m.IsActive)
            .OrderBy(m => m.User.FullName)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<RepairPlace>> GetRepairPlacesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.RepairPlaces
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<List<Client>> GetClientsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Clients
            .OrderBy(c => c.FullName)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}