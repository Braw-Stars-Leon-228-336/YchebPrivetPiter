using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class ReviewService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public ReviewService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<Review>> GetPagedAsync(
        int page = 1, int pageSize = 20,
        int? minRating = null,
        int? maxRating = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var q = db.Reviews.AsNoTracking()
            .Include(r => r.Client)
            .Include(r => r.WorkOrder)
            .AsQueryable();

        if (minRating.HasValue) q = q.Where(r => r.Rating >= minRating.Value);
        if (maxRating.HasValue) q = q.Where(r => r.Rating <= maxRating.Value);

        q = q.OrderByDescending(r => r.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Review>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<Review>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Reviews
            .Include(r => r.Client)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    /// <summary>Средний рейтинг по всем отзывам.</summary>
    public async Task<double> GetAverageRatingAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var list = await db.Reviews.Select(r => (double)r.Rating).ToListAsync(ct);
        return list.Count == 0 ? 0 : list.Average();
    }
}