using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class WorkOrderServices
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public WorkOrderServices(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<WorkOrder>> GetPagedAsync(
        int page = 1, int pageSize = 20,
        WorkOrderStatus? status = null,
        int? mechanicId = null,
        int? clientId = null,
        DateTime? from = null, DateTime? to = null,
        string sortBy = "date", bool desc = true,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var q = db.WorkOrders.AsNoTracking()
            .Include(w => w.Client)
            .Include(w => w.Car).ThenInclude(c => c.Brand)
            .Include(w => w.Mechanic).ThenInclude(m => m!.User)
            .AsQueryable();

        if (status.HasValue) q = q.Where(w => w.Status == status.Value);
        if (mechanicId.HasValue) q = q.Where(w => w.MechanicId == mechanicId.Value);
        if (clientId.HasValue) q = q.Where(w => w.ClientId == clientId.Value);
        if (from.HasValue) q = q.Where(w => w.OpenedAt >= from.Value);
        if (to.HasValue) q = q.Where(w => w.OpenedAt <= to.Value);

        q = sortBy switch
        {
            "status" => desc ? q.OrderByDescending(w => w.Status) : q.OrderBy(w => w.Status),
            "client" => desc ? q.OrderByDescending(w => w.Client.FullName) : q.OrderBy(w => w.Client.FullName),
            _ => desc ? q.OrderByDescending(w => w.OpenedAt) : q.OrderBy(w => w.OpenedAt),
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<WorkOrder>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<WorkOrder?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.WorkOrders
            .Include(w => w.Client)
            .Include(w => w.Car).ThenInclude(c => c.Brand)
            .Include(w => w.Mechanic).ThenInclude(m => m!.User)
            .Include(w => w.WorkOrderServices).ThenInclude(s => s.Service)
            .Include(w => w.WorkOrderParts).ThenInclude(p => p.Part)
            .Include(w => w.InspectionResults)
            .Include(w => w.Invoices)
            .FirstOrDefaultAsync(w => w.WorkOrderId == id, ct);
    }

    public async Task<WorkOrder> CreateAsync(WorkOrder w, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        w.OpenedAt = DateTime.UtcNow;
        db.WorkOrders.Add(w);
        await db.SaveChangesAsync(ct);
        return w;
    }

    /// <summary>
    /// Сохраняет заказ-наряд вместе с услугами и запчастями одной транзакцией.
    /// Корректно обрабатывает: создание/обновление заказа, добавление/удаление/изменение
    /// услуг и запчастей, списание/возврат запчастей со склада.
    /// </summary>
    public async Task<WorkOrder> SaveWithDetailsAsync(
        WorkOrder wo,
        IReadOnlyList<WorkOrderService> services,
        IReadOnlyList<WorkOrderPart> parts,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // ---------- 1. Сохраняем сам заказ ----------
        if (wo.WorkOrderId == 0)
        {
            wo.OpenedAt = DateTime.UtcNow;
            db.WorkOrders.Add(wo);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            var existingWo = await db.WorkOrders
                .FirstOrDefaultAsync(w => w.WorkOrderId == wo.WorkOrderId, ct)
                ?? throw new InvalidOperationException("Заказ не найден");

            existingWo.AppointmentId = wo.AppointmentId;
            existingWo.ClientId = wo.ClientId;
            existingWo.CarId = wo.CarId;
            existingWo.MechanicId = wo.MechanicId;
            existingWo.Status = wo.Status;
            existingWo.Description = wo.Description;
            existingWo.ClosedAt = wo.Status == WorkOrderStatus.completed && existingWo.ClosedAt is null
                                        ? DateTime.UtcNow
                                        : existingWo.ClosedAt;

            await db.SaveChangesAsync(ct);
        }

        // ---------- 2. Загружаем существующие детали ----------
        var existingServices = await db.WorkOrderServices
            .Where(s => s.WorkOrderId == wo.WorkOrderId).ToListAsync(ct);
        var existingParts = await db.WorkOrderParts
            .Where(p => p.WorkOrderId == wo.WorkOrderId).ToListAsync(ct);

        // ---------- 3. Синхронизация услуг ----------
        // Удаляем те, что исчезли из UI
        foreach (var es in existingServices)
        {
            if (!services.Any(s => s.Id == es.Id))
                db.WorkOrderServices.Remove(es);
        }

        // Обновляем/добавляем
        foreach (var s in services)
        {
            s.WorkOrderId = wo.WorkOrderId;
            if (s.Id == 0) db.WorkOrderServices.Add(s);
            else
            {
                var ex = existingServices.First(x => x.Id == s.Id);
                ex.ServiceId = s.ServiceId;
                ex.MechanicId = s.MechanicId;
                ex.Quantity = s.Quantity;
                ex.UnitPrice = s.UnitPrice;
            }
        }

        // ---------- 4. Синхронизация запчастей + склад ----------
        // Удаляем исчезнувшие — возвращаем на склад
        foreach (var ep in existingParts)
        {
            if (!parts.Any(p => p.Id == ep.Id))
            {
                var part = await db.Parts.FirstOrDefaultAsync(p => p.PartId == ep.PartId, ct);
                if (part is not null) part.StockQuantity += ep.Quantity;
                db.WorkOrderParts.Remove(ep);
            }
        }

        // Добавляем новые и обновляем существующие
        foreach (var p in parts)
        {
            p.WorkOrderId = wo.WorkOrderId;

            if (p.Id == 0)
            {
                var part = await db.Parts.FirstOrDefaultAsync(x => x.PartId == p.PartId, ct)
                    ?? throw new InvalidOperationException("Запчасть не найдена");

                if (part.StockQuantity < p.Quantity)
                    throw new InvalidOperationException(
                        $"Недостаточно «{part.Name}» на складе (доступно: {part.StockQuantity})");

                part.StockQuantity -= p.Quantity;
                db.WorkOrderParts.Add(p);
            }
            else
            {
                var ex = existingParts.First(x => x.Id == p.Id);
                var diff = p.Quantity - ex.Quantity;

                if (diff != 0)
                {
                    var part = await db.Parts.FirstOrDefaultAsync(x => x.PartId == p.PartId, ct);
                    if (part is not null)
                    {
                        if (part.StockQuantity < diff)
                            throw new InvalidOperationException(
                                $"Недостаточно «{part.Name}» на складе");
                        part.StockQuantity -= diff;
                    }
                }

                ex.PartId = p.PartId;
                ex.Quantity = p.Quantity;
                ex.UnitPrice = p.UnitPrice;
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return wo;
    }

    public async Task<bool> ChangeStatusAsync(int id, WorkOrderStatus status,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var w = await db.WorkOrders.FindAsync(new object?[] { id }, ct);
        if (w is null) return false;

        w.Status = status;
        if (status == WorkOrderStatus.completed && w.ClosedAt is null)
            w.ClosedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Добавить услугу в заказ-наряд.</summary>
    public async Task<WorkOrderService> AddServiceAsync(
        int workOrderId, int serviceId, int? mechanicId,
        int quantity, decimal unitPrice,
        CancellationToken ct = default)
    {
        if (quantity <= 0) throw new ArgumentException("Количество должно быть > 0");

        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.WorkOrderServices
            .FirstOrDefaultAsync(x =>
                x.WorkOrderId == workOrderId &&
                x.ServiceId == serviceId &&
                x.MechanicId == mechanicId, ct);

        if (existing != null)
        {
            existing.Quantity += quantity;
            existing.UnitPrice = unitPrice;
            await db.SaveChangesAsync(ct);
            return existing;
        }

        var item = new WorkOrderService
        {
            WorkOrderId = workOrderId,
            ServiceId = serviceId,
            MechanicId = mechanicId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
        db.WorkOrderServices.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    /// <summary>
    /// Добавить запчасть с проверкой остатка и списанием со склада.
    /// </summary>
    public async Task<(bool Ok, string? Error, WorkOrderPart? Item)> AddPartAsync(
        int workOrderId, int partId, int quantity, decimal? unitPrice = null,
        CancellationToken ct = default)
    {
        if (quantity <= 0)
            return (false, "Количество должно быть больше нуля", null);

        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var part = await db.Parts.FirstOrDefaultAsync(p => p.PartId == partId, ct);
        if (part is null)
            return (false, "Запчасть не найдена", null);

        if (part.StockQuantity < quantity)
            return (false, $"Недостаточно на складе (доступно: {part.StockQuantity})", null);

        var existing = await db.WorkOrderParts
            .FirstOrDefaultAsync(x => x.WorkOrderId == workOrderId && x.PartId == partId, ct);

        WorkOrderPart item;
        if (existing != null)
        {
            existing.Quantity += quantity;
            if (unitPrice.HasValue) existing.UnitPrice = unitPrice.Value;
            item = existing;
        }
        else
        {
            item = new WorkOrderPart
            {
                WorkOrderId = workOrderId,
                PartId = partId,
                Quantity = quantity,
                UnitPrice = unitPrice ?? part.SalePrice
            };
            db.WorkOrderParts.Add(item);
        }

        part.StockQuantity -= quantity;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (true, null, item);
    }

    /// <summary>Удалить запчасть из заказа и вернуть её на склад.</summary>
    public async Task<bool> RemovePartAsync(int workOrderPartId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var item = await db.WorkOrderParts
            .Include(x => x.Part)
            .FirstOrDefaultAsync(x => x.Id == workOrderPartId, ct);
        if (item is null) return false;

        item.Part.StockQuantity += item.Quantity;
        db.WorkOrderParts.Remove(item);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> RemoveServiceAsync(int workOrderServiceId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var item = await db.WorkOrderServices
            .FirstOrDefaultAsync(x => x.Id == workOrderServiceId, ct);
        if (item is null) return false;

        db.WorkOrderServices.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Итоговая стоимость заказа (услуги + запчасти).</summary>
    public async Task<decimal> GetTotalAsync(int workOrderId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var services = await db.WorkOrderServices
            .Where(s => s.WorkOrderId == workOrderId)
            .SumAsync(s => (decimal?)(s.Quantity * s.UnitPrice), ct) ?? 0m;

        var parts = await db.WorkOrderParts
            .Where(p => p.WorkOrderId == workOrderId)
            .SumAsync(p => (decimal?)(p.Quantity * p.UnitPrice), ct) ?? 0m;

        return services + parts;
    }
    // ---------- Справочники ----------
    public async Task<List<Client>> GetClientsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Clients.OrderBy(c => c.FullName).AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<Service>> GetServicesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Services.Where(s => s.IsActive).OrderBy(s => s.Name).AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<Part>> GetPartsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Parts.Where(p => p.IsActive).OrderBy(p => p.Name).AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<Mechanic>> GetMechanicsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Mechanics.Include(m => m.User).Where(m => m.IsActive)
            .OrderBy(m => m.User.FullName).AsNoTracking().ToListAsync(ct);
    }
}
