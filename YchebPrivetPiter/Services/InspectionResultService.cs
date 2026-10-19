using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class InspectionResultService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public InspectionResultService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>Последний осмотр по заказу (или null).</summary>
    public async Task<InspectionResult?> GetByWorkOrderAsync(int workOrderId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.InspectionResults
            .Include(i => i.Mechanic).ThenInclude(m => m.User)
            .Where(i => i.WorkOrderId == workOrderId)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<InspectionResult>> GetListByWorkOrderAsync(int workOrderId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.InspectionResults
            .Include(i => i.Mechanic).ThenInclude(m => m.User)
            .Where(i => i.WorkOrderId == workOrderId)
            .OrderByDescending(i => i.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<InspectionResult> CreateAsync(InspectionResult r, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        r.CreatedAt = DateTime.UtcNow;
        db.InspectionResults.Add(r);
        await db.SaveChangesAsync(ct);
        return r;
    }

    public async Task<InspectionResult> UpdateAsync(InspectionResult r, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var existing = await db.InspectionResults
            .FirstOrDefaultAsync(x => x.InspectionId == r.InspectionId, ct);
        if (existing is null) return r;

        existing.MechanicId = r.MechanicId;
        existing.EngineCondition = r.EngineCondition;
        existing.BrakeCondition = r.BrakeCondition;
        existing.SuspensionCondition = r.SuspensionCondition;
        existing.ElectricalCondition = r.ElectricalCondition;
        existing.Recommendations = r.Recommendations;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var r = await db.InspectionResults.FindAsync(new object?[] { id }, ct);
        if (r is null) return false;
        db.InspectionResults.Remove(r);
        await db.SaveChangesAsync(ct);
        return true;
    }
}