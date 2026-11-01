using NTA.Core.ValueObjects;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Models;

namespace NTA.Infrastructure.Sqlite.Mappings;

/// <summary>
/// Translates between SQLite user records and user aggregates.
/// </summary>
internal static class SqliteUserMapper
{
    /// <summary>
    /// Converts a SQLite user record to a domain user aggregate.
    /// </summary>
    public static User ToDomain(SqliteUser persistence) => new(new UserId(persistence.Id), persistence.Name);

    /// <summary>
    /// Converts a user aggregate to a SQLite user record.
    /// </summary>
    public static SqliteUser ToPersistence(User user) => new()
    {
        Id = user.Id.Value,
        Name = user.Name
    };

    /// <summary>
    /// Copies mutable aggregate state into an existing SQLite record.
    /// </summary>
    public static void Apply(User user, SqliteUser persistence) => persistence.Name = user.Name;
}
