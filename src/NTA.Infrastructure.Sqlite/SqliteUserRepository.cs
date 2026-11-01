using Microsoft.EntityFrameworkCore;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Mappings;

namespace NTA.Infrastructure.Sqlite;

/// <summary>
/// Retrieves seeded user aggregates from SQLite.
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
}
