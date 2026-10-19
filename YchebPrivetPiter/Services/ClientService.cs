using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class ClientService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    public ClientService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<PagedResult<Client>> GetPagedAsync(
        int page = 1, int pageSize = 20,
        string? search = null,
        string sortBy = "name", bool desc = false,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var q = db.Clients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(c =>
                EF.Functions.ILike(c.FullName, s) ||
                EF.Functions.ILike(c.Phone, s) ||
                (c.Email != null && EF.Functions.ILike(c.Email, s)));
        }

        q = sortBy switch
        {
            "phone" => desc ? q.OrderByDescending(c => c.Phone) : q.OrderBy(c => c.Phone),
            "email" => desc ? q.OrderByDescending(c => c.Email) : q.OrderBy(c => c.Email),
            "created" => desc ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.CreatedAt),
            _ => desc ? q.OrderByDescending(c => c.FullName) : q.OrderBy(c => c.FullName),
        };

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<Client>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Client?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Clients
            .Include(c => c.Cars).ThenInclude(car => car.Brand)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClientId == id, ct);
    }

    public async Task<Client> CreateAsync(Client client, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        client.CreatedAt = DateTime.UtcNow;
        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return client;
    }

    public async Task<Client> UpdateAsync(Client client, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var existing = await db.Clients
            .FirstOrDefaultAsync(c => c.ClientId == client.ClientId, ct);
        if (existing is null) return client;

        existing.FullName = client.FullName;
        existing.Phone = client.Phone;
        existing.Email = client.Email;
        existing.Address = client.Address;
        existing.BirthDate = client.BirthDate;
        existing.Notes = client.Notes;
        // CreatedAt не трогаем — оно уже есть в БД

        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var c = await db.Clients.FindAsync(new object?[] { id }, ct);
        if (c is null) return false;
        db.Clients.Remove(c);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null,
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var q = db.Clients.Where(c => c.Phone == phone);
        if (excludeId.HasValue) q = q.Where(c => c.ClientId != excludeId.Value);
        return await q.AnyAsync(ct);
    }
}