using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class InvoiceService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public InvoiceService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>
    /// Сформировать счёт на основе заказ-наряда.
    /// Subtotal считается как сумма услуг и запчастей. Налог и скидка — параметры.
    /// TotalAmount — computed column, после SaveChanges подгружаем через Reload.
    /// </summary>
    public async Task<Invoice> CreateFromWorkOrderAsync(
        int workOrderId,
        decimal discountAmount = 0m,
        decimal taxRate = 0.20m,
        DateOnly? dueDate = null,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var wo = await db.WorkOrders
            .Include(w => w.WorkOrderServices)
            .Include(w => w.WorkOrderParts)
            .FirstOrDefaultAsync(w => w.WorkOrderId == workOrderId, ct)
            ?? throw new InvalidOperationException("Заказ-наряд не найден");

        var servicesSum = wo.WorkOrderServices.Sum(s => s.Quantity * s.UnitPrice);
        var partsSum = wo.WorkOrderParts.Sum(p => p.Quantity * p.UnitPrice);
        var subtotal = servicesSum + partsSum;

        var invoice = new Invoice
        {
            WorkOrderId = workOrderId,
            ClientId = wo.ClientId,
            Number = await GenerateNumberAsync(db, ct),
            CreatedAt = DateTime.UtcNow,
            DueDate = dueDate,
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            TaxRate = taxRate,
            Status = InvoiceStatus.unpaid
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        // TotalAmount — сгенерированная колонка, подтянем её значение
        await db.Entry(invoice).ReloadAsync(ct);
        return invoice;
    }

    public async Task<PagedResult<Invoice>> GetPagedAsync(
        int page = 1, int pageSize = 20,
        InvoiceStatus? status = null,
        int? clientId = null,
        string sortBy = "date", bool desc = true,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var q = db.Invoices.AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.WorkOrder)
            .AsQueryable();

        if (status.HasValue) q = q.Where(i => i.Status == status.Value);
        if (clientId.HasValue) q = q.Where(i => i.ClientId == clientId.Value);

        q = sortBy switch
        {
            "amount" => desc ? q.OrderByDescending(i => i.TotalAmount) : q.OrderBy(i => i.TotalAmount),
            "status" => desc ? q.OrderByDescending(i => i.Status) : q.OrderBy(i => i.Status),
            _ => desc ? q.OrderByDescending(i => i.CreatedAt) : q.OrderBy(i => i.CreatedAt),
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Invoice>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Invoice?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Invoices
            .Include(i => i.Client)
            .Include(i => i.Payments)
            .Include(i => i.WorkOrder)
            .FirstOrDefaultAsync(i => i.InvoiceId == id, ct);
    }

    /// <summary>Пересчитать статус счёта по сумме завершённых платежей.</summary>
    public async Task<bool> RecalculateStatusAsync(int invoiceId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var invoice = await db.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, ct);
        if (invoice is null) return false;

        var paid = invoice.Payments
            .Where(p => p.Status == PaymentStatus.completed)
            .Sum(p => p.Amount);

        var total = invoice.TotalAmount ?? 0m;

        if (paid >= total) invoice.Status = InvoiceStatus.paid;
        else if (paid > 0) invoice.Status = InvoiceStatus.partially_paid;
        else invoice.Status = InvoiceStatus.unpaid;

        await db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task<string> GenerateNumberAsync(AppDbContext db, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.Invoices.CountAsync(i => i.CreatedAt.Year == year, ct);
        return $"INV-{year}-{(count + 1):D4}";
    }
}