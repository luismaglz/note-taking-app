using Microsoft.EntityFrameworkCore;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Mappings;

namespace NTA.Infrastructure.Sqlite;

/// <summary>
/// Persists user aggregates in SQLite.
/// </summary>
public sealed class SqliteUserRepository(NoteDbContext dbContext) : IUserRepository
{
    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var persistence = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id.Value, cancellationToken);

        return persistence is null ? null : SqliteUserMapper.ToDomain(persistence);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Id)
            .ToListAsync(cancellationToken))
        .Select(SqliteUserMapper.ToDomain)
        .ToArray();

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        await dbContext.Users.AddAsync(SqliteUserMapper.ToPersistence(user), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var persistence = await dbContext.Users.SingleOrDefaultAsync(
            existing => existing.Id == user.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"User '{user.Id.Value}' does not exist.");

        SqliteUserMapper.Apply(user, persistence);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var persistence = await dbContext.Users.SingleOrDefaultAsync(
            user => user.Id == id.Value,
            cancellationToken);

        if (persistence is null)
        {
            return false;
        }

        dbContext.Users.Remove(persistence);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
