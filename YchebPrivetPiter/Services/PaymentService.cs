using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class PaymentService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public PaymentService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>
    /// Зарегистрировать платёж по счёту и автоматически пересчитать его статус.
    /// </summary>
    public async Task<Payment> RegisterAsync(
        int invoiceId, decimal amount, PaymentMethod method,
        string? transactionNumber = null,
        PaymentStatus status = PaymentStatus.completed,
        CancellationToken ct = default)
    {
        if (amount <= 0)
            throw new ArgumentException("Сумма должна быть больше нуля");

        await using var db = await _factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var invoice = await db.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, ct)
            ?? throw new InvalidOperationException("Счёт не найден");

        var payment = new Payment
        {
            InvoiceId = invoiceId,
            PaidAt = DateTime.UtcNow,
            Amount = amount,
            Method = method,
            Status = status,
            TransactionNumber = transactionNumber
        };
        db.Payments.Add(payment);

        var paidSoFar = invoice.Payments
            .Where(p => p.Status == PaymentStatus.completed)
            .Sum(p => p.Amount)
            + (status == PaymentStatus.completed ? amount : 0m);

        var total = invoice.TotalAmount ?? 0m;

        if (paidSoFar >= total) invoice.Status = InvoiceStatus.paid;
        else if (paidSoFar > 0) invoice.Status = InvoiceStatus.partially_paid;
        else invoice.Status = InvoiceStatus.unpaid;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return payment;
    }

    public async Task<List<Payment>> GetByInvoiceAsync(int invoiceId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Payments
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.PaidAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Payment>> GetPagedAsync(
        int page = 1, int pageSize = 20,
        DateTime? from = null, DateTime? to = null,
        PaymentMethod? method = null,
        PaymentStatus? status = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var q = db.Payments.AsNoTracking().Include(p => p.Invoice).AsQueryable();

        if (from.HasValue) q = q.Where(p => p.PaidAt >= from.Value);
        if (to.HasValue) q = q.Where(p => p.PaidAt <= to.Value);
        if (method.HasValue) q = q.Where(p => p.Method == method.Value);
        if (status.HasValue) q = q.Where(p => p.Status == status.Value);

        q = q.OrderByDescending(p => p.PaidAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Payment>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

}