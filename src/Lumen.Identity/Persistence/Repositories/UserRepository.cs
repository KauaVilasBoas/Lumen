using Lumen.Identity.Domain.Users;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Lumen.Identity.Persistence.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private const int SqlServerUniqueConstraintViolation = 2627;
    private const int SqlServerUniqueIndexViolation = 2601;

    private readonly IdentityDbContext _dbContext;

    public UserRepository(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> FindByIdIgnoringFiltersAsync(Guid id, CancellationToken ct = default)
        => _dbContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> FindByUsernameAsync(string username, CancellationToken ct = default)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task InsertAsync(User user, CancellationToken ct = default)
    {
        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsSqlUniqueViolation(ex, out var message))
        {
            if (IsEmailIndex(message))
                throw new DuplicateEmailException(user.Email);

            throw new DuplicateUsernameException(user.Username);
        }
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<User> Items, int Total)> ListAsync(
        string? search,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _dbContext.Users.IgnoreQueryFilters()
            : _dbContext.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    private static bool IsSqlUniqueViolation(DbUpdateException ex, out string message)
    {
        if (ex.InnerException is SqlException { Number: SqlServerUniqueConstraintViolation or SqlServerUniqueIndexViolation } sqlEx)
        {
            message = sqlEx.Message;
            return true;
        }

        message = string.Empty;
        return false;
    }

    private static bool IsEmailIndex(string errorMessage)
        => errorMessage.Contains("ix_identity_users_email_unique", StringComparison.OrdinalIgnoreCase)
        || errorMessage.Contains("\"email\"", StringComparison.OrdinalIgnoreCase);
}
