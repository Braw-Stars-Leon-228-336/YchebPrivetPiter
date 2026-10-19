using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class CarService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public CarService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<Car>> GetPagedAsync(
        int page = 1, int pageSize = 20,
        string? search = null,
        int? clientId = null,
        int? brandId = null,
        string sortBy = "model", bool desc = false,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var q = db.Cars.AsNoTracking()
            .Include(c => c.Brand)
            .Include(c => c.Category)
            .Include(c => c.Client)
            .AsQueryable();

        if (clientId.HasValue) q = q.Where(c => c.ClientId == clientId.Value);
        if (brandId.HasValue) q = q.Where(c => c.BrandId == brandId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(c =>
                EF.Functions.ILike(c.Vin, s) ||
                EF.Functions.ILike(c.PlateNumber, s) ||
                EF.Functions.ILike(c.Model, s) ||
                EF.Functions.ILike(c.Brand.Name, s));
        }

        q = sortBy switch
        {
            "mileage" => desc ? q.OrderByDescending(c => c.Mileage) : q.OrderBy(c => c.Mileage),
            "year" => desc ? q.OrderByDescending(c => c.ProductionYear) : q.OrderBy(c => c.ProductionYear),
            "plate" => desc ? q.OrderByDescending(c => c.PlateNumber) : q.OrderBy(c => c.PlateNumber),
            "brand" => desc ? q.OrderByDescending(c => c.Brand.Name) : q.OrderBy(c => c.Brand.Name),
            _ => desc ? q.OrderByDescending(c => c.Model) : q.OrderBy(c => c.Model),
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Car>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Car?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Cars
            .Include(c => c.Brand)
            .Include(c => c.Category)
            .Include(c => c.Client)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CarId == id, ct);
    }

    public async Task<List<Car>> GetByClientAsync(int clientId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Cars
            .Where(c => c.ClientId == clientId)
            .Include(c => c.Brand)
            .OrderBy(c => c.Model)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<Car> CreateAsync(Car car, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        car.CreatedAt = DateTime.UtcNow;
        db.Cars.Add(car);
        await db.SaveChangesAsync(ct);
        return car;
    }

    public async Task<Car> UpdateAsync(Car car, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.Cars.FirstOrDefaultAsync(c => c.CarId == car.CarId, ct);
        if (existing is null) return car;

        existing.ClientId = car.ClientId;
        existing.BrandId = car.BrandId;
        existing.CategoryId = car.CategoryId;
        existing.Model = car.Model;
        existing.ProductionYear = car.ProductionYear;
        existing.Vin = car.Vin;
        existing.PlateNumber = car.PlateNumber;
        existing.Mileage = car.Mileage;
        existing.Color = car.Color;

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var c = await db.Cars.FindAsync(new object?[] { id }, ct);
        if (c is null) return false;
        db.Cars.Remove(c);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<CarBrand>> GetBrandsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.CarBrands.OrderBy(b => b.Name).AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<CarCategory>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.CarCategories.OrderBy(c => c.Name).AsNoTracking().ToListAsync(ct);
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