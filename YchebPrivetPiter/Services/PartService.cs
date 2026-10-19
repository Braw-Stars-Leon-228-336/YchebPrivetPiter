using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class PartService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public PartService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<Part>> GetPagedAsync(
        int page = 1, int pageSize = 20,
        string? search = null, int? supplierId = null,
        bool onlyLowStock = false,
        string sortBy = "name", bool desc = false,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var q = db.Parts.AsNoTracking().Include(p => p.Supplier).AsQueryable();

        if (supplierId.HasValue) q = q.Where(p => p.SupplierId == supplierId.Value);
        if (onlyLowStock) q = q.Where(p => p.StockQuantity < p.MinStock);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(p => EF.Functions.ILike(p.Name, s) || EF.Functions.ILike(p.Article, s));
        }

        q = sortBy switch
        {
            "stock" => desc ? q.OrderByDescending(p => p.StockQuantity) : q.OrderBy(p => p.StockQuantity),
            "price" => desc ? q.OrderByDescending(p => p.SalePrice) : q.OrderBy(p => p.SalePrice),
            _ => desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Part>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Part?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Parts.Include(p => p.Supplier).FirstOrDefaultAsync(p => p.PartId == id, ct);
    }

    public async Task<List<Part>> GetLowStockAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Parts
            .Where(p => p.StockQuantity < p.MinStock)
            .Include(p => p.Supplier)
            .OrderBy(p => p.StockQuantity)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> HasStockAsync(int partId, int qty, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Parts.AnyAsync(p => p.PartId == partId && p.StockQuantity >= qty, ct);
    }

    public async Task<Part> CreateAsync(Part part, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Parts.Add(part);
        await db.SaveChangesAsync(ct);
        return part;
    }

    public async Task<Part> UpdateAsync(Part part, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Parts.Update(part);
        await db.SaveChangesAsync(ct);
        return part;
    }

    public async Task<bool> AdjustStockAsync(int partId, int delta, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var p = await db.Parts.FindAsync(new object?[] { partId }, ct);
        if (p is null) return false;

        var newQty = p.StockQuantity + delta;
        if (newQty < 0) return false;

        p.StockQuantity = newQty;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<Supplier>> GetSuppliersAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Suppliers.OrderBy(s => s.Name).AsNoTracking().ToListAsync(ct);
    }
}