using Microsoft.EntityFrameworkCore;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Mappings;

namespace NTA.Infrastructure.Sqlite;

/// <summary>
/// Retrieves team membership information from SQLite.
/// </summary>
public sealed class SqliteTeamRepository(NoteDbContext dbContext) : ITeamRepository
{
    public async Task<IReadOnlyList<Team>> ListByMemberAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var teamIds = await dbContext.TeamMemberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId.Value)
            .Select(membership => membership.TeamId)
            .ToListAsync(cancellationToken);
        var teams = await dbContext.Teams
            .AsNoTracking()
            .Where(team => teamIds.Contains(team.Id))
            .ToListAsync(cancellationToken);
        var memberships = await dbContext.TeamMemberships
            .AsNoTracking()
            .Where(membership => teamIds.Contains(membership.TeamId))
            .ToListAsync(cancellationToken);

        return teams.Select(team => SqliteTeamMapper.ToDomain(team, memberships)).ToArray();
    }
}
