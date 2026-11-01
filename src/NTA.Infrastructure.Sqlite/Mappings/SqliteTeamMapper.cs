using NTA.Core.ValueObjects;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Models;

namespace NTA.Infrastructure.Sqlite.Mappings;

/// <summary>
/// Translates between SQLite team records and team aggregates.
/// </summary>
internal static class SqliteTeamMapper
{
    public static Team ToDomain(SqliteTeam team, IEnumerable<SqliteTeamMembership> memberships) =>
        new(
            new TeamId(team.Id),
            memberships
                .Where(membership => membership.TeamId == team.Id)
                .Select(membership => new UserId(membership.UserId)));
}
