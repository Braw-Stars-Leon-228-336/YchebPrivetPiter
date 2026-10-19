using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;
using YchebPrivetPiter.Services;


namespace YchebPrivetPiter.Services;

public class AnalyticsService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AnalyticsService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    // ---------- KPI ----------
    public async Task<int> GetOrdersCountAsync(DateTime? from = null, DateTime? to = null,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var q = db.WorkOrders.AsQueryable();
        if (from.HasValue) q = q.Where(w => w.OpenedAt >= from.Value);
        if (to.HasValue) q = q.Where(w => w.OpenedAt <= to.Value);
        return await q.CountAsync(ct);
    }

    public async Task<decimal> GetRevenueAsync(DateTime? from = null, DateTime? to = null,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var q = db.Payments.Where(p => p.Status == PaymentStatus.completed);
        if (from.HasValue) q = q.Where(p => p.PaidAt >= from.Value);
        if (to.HasValue) q = q.Where(p => p.PaidAt <= to.Value);
        return await q.SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
    }

    public async Task<decimal> GetAverageRepairCostAsync(DateTime? from = null, DateTime? to = null,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var q = db.Invoices.AsQueryable();
        if (from.HasValue) q = q.Where(i => i.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(i => i.CreatedAt <= to.Value);
        return await q.AverageAsync(i => (decimal?)i.TotalAmount, ct) ?? 0m;
    }

    // ---------- Топ услуг ----------
    public async Task<List<TopServiceDto>> GetTopServicesAsync(int top = 5,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.WorkOrderServices
            .GroupBy(x => new { x.ServiceId, x.Service.Name })
            .Select(g => new TopServiceDto
            {
                ServiceId = g.Key.ServiceId,
                Name = g.Key.Name,
                TimesUsed = g.Count(),
                Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.TimesUsed)
            .Take(top)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // ---------- Загруженность механиков ----------
    public async Task<List<MechanicWorkloadDto>> GetMechanicsWorkloadAsync(
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.WorkOrders
            .Where(w => w.MechanicId != null)
            .GroupBy(w => new { w.MechanicId, Name = w.Mechanic!.User.FullName })
            .Select(g => new MechanicWorkloadDto
            {
                MechanicId = g.Key.MechanicId!.Value,
                MechanicName = g.Key.Name,
                OrdersCount = g.Count(),
                CompletedCount = g.Count(x => x.Status == WorkOrderStatus.completed)
            })
            .OrderByDescending(x => x.OrdersCount)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // ---------- Запчасти ниже минимума ----------
    public async Task<List<Part>> GetLowStockPartsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Parts
            .Where(p => p.StockQuantity < p.MinStock)
            .OrderBy(p => p.StockQuantity)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // ---------- Клиенты с повторными обращениями ----------
    public async Task<List<RepeatClientDto>> GetRepeatClientsAsync(int minVisits = 2,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.WorkOrders
            .GroupBy(w => new { w.ClientId, Name = w.Client.FullName, Phone = w.Client.Phone })
            .Where(g => g.Count() >= minVisits)
            .Select(g => new RepeatClientDto
            {
                ClientId = g.Key.ClientId,
                FullName = g.Key.Name,
                Phone = g.Key.Phone,
                Visits = g.Count()
            })
            .OrderByDescending(x => x.Visits)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // ---------- Услуги дороже средней ----------
    public async Task<List<Service>> GetServicesAboveAverageAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var avg = await db.Services.AverageAsync(s => s.BasePrice, ct);
        return await db.Services
            .Where(s => s.BasePrice > avg)
            .OrderByDescending(s => s.BasePrice)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // ---------- Механики с числом заказов выше среднего ----------
    public async Task<List<MechanicWorkloadDto>> GetMechanicsAboveAverageAsync(
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var counts = await db.WorkOrders
            .Where(w => w.MechanicId != null)
            .GroupBy(w => w.MechanicId)
            .Select(g => g.Count())
            .ToListAsync(ct);

        if (counts.Count == 0) return new List<MechanicWorkloadDto>();

        var avg = counts.Average();

        return await db.WorkOrders
            .Where(w => w.MechanicId != null)
            .GroupBy(w => new { w.MechanicId, Name = w.Mechanic!.User.FullName })
            .Where(g => g.Count() > avg)
            .Select(g => new MechanicWorkloadDto
            {
                MechanicId = g.Key.MechanicId!.Value,
                MechanicName = g.Key.Name,
                OrdersCount = g.Count(),
                CompletedCount = g.Count(x => x.Status == WorkOrderStatus.completed)
            })
            .OrderByDescending(x => x.OrdersCount)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    // ============================================================
    //  ROLLUP / CUBE (raw SQL)
    // ============================================================

    public async Task<List<RevenueRollupDto>> GetRevenueRollupAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT EXTRACT(YEAR  FROM paid_at)::int AS yr,
                   EXTRACT(MONTH FROM paid_at)::int AS mn,
                   SUM(amount)::numeric             AS revenue
            FROM payments
            WHERE status = 'completed'
            GROUP BY ROLLUP (yr, mn)
            ORDER BY yr NULLS LAST, mn NULLS LAST;";

        await using var db = await _factory.CreateDbContextAsync(ct);
        var result = new List<RevenueRollupDto>();

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(new RevenueRollupDto
                {
                    Year = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                    Month = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    Revenue = reader.IsDBNull(2) ? 0m : reader.GetDecimal(2)
                });
            }
        }
        finally
        {
            await conn.CloseAsync();
        }

        return result;
    }

    public async Task<List<OrdersCubeDto>> GetOrdersCubeAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT m.mechanic_id,
                   u.full_name          AS mechanic_name,
                   wo.status::text      AS status,
                   COUNT(*)::int        AS cnt
            FROM work_orders wo
            JOIN mechanics m ON m.mechanic_id = wo.mechanic_id
            JOIN users     u ON u.user_id     = m.user_id
            GROUP BY CUBE (m.mechanic_id, u.full_name, wo.status)
            ORDER BY m.mechanic_id NULLS LAST, wo.status NULLS LAST;";

        await using var db = await _factory.CreateDbContextAsync(ct);
        var result = new List<OrdersCubeDto>();

        var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(ct);

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(new OrdersCubeDto
                {
                    MechanicId = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                    MechanicName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Status = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Count = reader.IsDBNull(3) ? 0 : reader.GetInt32(3)
                });
            }
        }
        finally
        {
            await conn.CloseAsync();
        }

        return result;
    }
    /// <summary>Средний рейтинг клиентов.</summary>
    public async Task<double> GetAverageRatingAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var list = await db.Reviews.Select(r => (double)r.Rating).ToListAsync(ct);
        return list.Count == 0 ? 0 : list.Average();
    }

    /// <summary>Количество отзывов.</summary>
    public async Task<int> GetReviewsCountAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Reviews.CountAsync(ct);
    }

    /// <summary>Выручка по дням за последние N дней (для графика).</summary>
    public async Task<List<DailyRevenueDto>> GetDailyRevenueAsync(int days = 30,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var from = DateTime.UtcNow.AddDays(-days);

        var rows = await db.Payments
            .Where(p => p.Status == PaymentStatus.completed && p.PaidAt >= from)
            .GroupBy(p => p.PaidAt.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.Amount),
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .AsNoTracking()
            .ToListAsync(ct);

        return rows;
    }
}