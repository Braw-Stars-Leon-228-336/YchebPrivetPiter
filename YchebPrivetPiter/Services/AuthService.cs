using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YchebPrivetPiter.Data;

namespace YchebPrivetPiter.Services;

public class AuthService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public AuthService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    /// <summary>
    /// Проверяет логин/пароль через pgcrypto: crypt(password, password_hash).
    /// Возвращает User с подгруженными Role и Mechanic, либо null.
    /// </summary>
    public async Task<User?> AuthenticateAsync(string login, string password,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
            return null;

        await using var db = await _factory.CreateDbContextAsync(ct);

        var users = await db.Users
            .FromSqlInterpolated($@"
                SELECT * FROM users
                WHERE login = {login}
                  AND password_hash = crypt({password}, password_hash)
                  AND is_active = TRUE")
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Include(u => u.Mechanic)
                .ThenInclude(m => m!.Specialization)
            .AsNoTracking()
            .ToListAsync(ct);

        return users.FirstOrDefault();
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Users
            .Include(u => u.Role)
            .Include(u => u.Department)
            .Include(u => u.Mechanic)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);
    }
}