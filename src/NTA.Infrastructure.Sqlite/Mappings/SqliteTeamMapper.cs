using NTA.Core.ValueObjects;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Models;

namespace NTA.Infrastructure.Sqlite.Mappings;

/// <summary>
/// Translates between SQLite team records and team aggregates.
/// </summary>
internal static class SqliteTeamMapper
{
    /// <summary>
    /// Converts SQLite team and membership records to a domain team aggregate.
    /// </summary>
    public static Team ToDomain(SqliteTeam team, IEnumerable<SqliteTeamMembership> members) =>
        new(new TeamId(team.Id), team.Name, members.Select(member => new UserId(member.UserId)));

    /// <summary>
    /// Converts a team aggregate to a SQLite team record.
    /// </summary>
    public static SqliteTeam ToPersistence(Team team) => new()
    {
        Id = team.Id.Value,
        Name = team.Name
    };

    /// <summary>
    /// Converts team membership to SQLite membership records.
    /// </summary>
    public static IEnumerable<SqliteTeamMembership> ToMembershipPersistence(Team team) =>
        team.Members.Select(member => new SqliteTeamMembership
        {
            TeamId = team.Id.Value,
            UserId = member.Value
        });

    /// <summary>
    /// Copies mutable aggregate state into an existing SQLite team record.
    /// </summary>
    public static void Apply(Team team, SqliteTeam persistence) => persistence.Name = team.Name;
}
