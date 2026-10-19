using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class MechanicScheduleService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public MechanicScheduleService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<MechanicSchedule>> GetPagedAsync(
        int page = 1, int pageSize = 20,
        DateTime? from = null, DateTime? to = null,
        int? mechanicId = null,
        ScheduleStatus? status = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var q = db.MechanicSchedules.AsNoTracking()
            .Include(s => s.Mechanic).ThenInclude(m => m.User)
            .Include(s => s.RepairPlace)
            .AsQueryable();

        if (from.HasValue) q = q.Where(s => s.WorkDate >= DateOnly.FromDateTime(from.Value));
        if (to.HasValue) q = q.Where(s => s.WorkDate <= DateOnly.FromDateTime(to.Value));
        if (mechanicId.HasValue) q = q.Where(s => s.MechanicId == mechanicId.Value);
        if (status.HasValue) q = q.Where(s => s.Status == status.Value);

        q = q.OrderBy(s => s.WorkDate).ThenBy(s => s.StartTime);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<MechanicSchedule>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MechanicSchedule?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.MechanicSchedules
            .Include(s => s.Mechanic).ThenInclude(m => m.User)
            .Include(s => s.RepairPlace)
            .FirstOrDefaultAsync(s => s.ScheduleId == id, ct);
    }

    public async Task<MechanicSchedule> CreateAsync(MechanicSchedule s, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.MechanicSchedules.Add(s);
        await db.SaveChangesAsync(ct);
        return s;
    }

    public async Task<MechanicSchedule> UpdateAsync(MechanicSchedule s, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.MechanicSchedules
            .FirstOrDefaultAsync(x => x.ScheduleId == s.ScheduleId, ct);
        if (existing is null) return s;

        existing.MechanicId = s.MechanicId;
        existing.RepairPlaceId = s.RepairPlaceId;
        existing.WorkDate = s.WorkDate;
        existing.StartTime = s.StartTime;
        existing.EndTime = s.EndTime;
        existing.Status = s.Status;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var s = await db.MechanicSchedules.FindAsync(new object?[] { id }, ct);
        if (s is null) return false;
        db.MechanicSchedules.Remove(s);
        await db.SaveChangesAsync(ct);
        return true;
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
}