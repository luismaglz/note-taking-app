using Microsoft.EntityFrameworkCore;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Mappings;

namespace NTA.Infrastructure.Sqlite;

/// <summary>
/// Persists team aggregates and their memberships in SQLite.
/// </summary>
public sealed class SqliteTeamRepository(NoteDbContext dbContext) : ITeamRepository
{
    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<Team?> GetByIdAsync(TeamId id, CancellationToken cancellationToken = default)
    {
        var team = await dbContext.Teams.AsNoTracking().SingleOrDefaultAsync(
            entity => entity.Id == id.Value,
            cancellationToken);

        if (team is null)
        {
            return null;
        }

        var members = await dbContext.TeamMemberships.AsNoTracking()
            .Where(member => member.TeamId == id.Value)
            .ToListAsync(cancellationToken);
        return SqliteTeamMapper.ToDomain(team, members);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Team>> ListAsync(CancellationToken cancellationToken = default)
    {
        var teams = await dbContext.Teams.AsNoTracking().OrderBy(team => team.Id).ToListAsync(cancellationToken);
        var memberships = await dbContext.TeamMemberships.AsNoTracking().ToListAsync(cancellationToken);

        return teams.Select(team => SqliteTeamMapper.ToDomain(
            team,
            memberships.Where(member => member.TeamId == team.Id))).ToArray();
    }

    /// <inheritdoc />
    public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(team);
        await dbContext.Teams.AddAsync(SqliteTeamMapper.ToPersistence(team), cancellationToken);
        await dbContext.TeamMemberships.AddRangeAsync(
            SqliteTeamMapper.ToMembershipPersistence(team),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Team team, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(team);

        var persistence = await dbContext.Teams.SingleOrDefaultAsync(
            existing => existing.Id == team.Id.Value,
            cancellationToken)
            ?? throw new InvalidOperationException($"Team '{team.Id.Value}' does not exist.");

        SqliteTeamMapper.Apply(team, persistence);
        var memberships = await dbContext.TeamMemberships
            .Where(member => member.TeamId == team.Id.Value)
            .ToListAsync(cancellationToken);

        var currentMemberIds = memberships.Select(member => member.UserId).ToHashSet();
        dbContext.TeamMemberships.RemoveRange(memberships.Where(member => !team.Members.Contains(new UserId(member.UserId))));
        await dbContext.TeamMemberships.AddRangeAsync(
            SqliteTeamMapper.ToMembershipPersistence(team)
                .Where(member => !currentMemberIds.Contains(member.UserId)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(TeamId id, CancellationToken cancellationToken = default)
    {
        var persistence = await dbContext.Teams.SingleOrDefaultAsync(
            team => team.Id == id.Value,
            cancellationToken);

        if (persistence is null)
        {
            return false;
        }

        dbContext.Teams.Remove(persistence);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public Task<bool> HasMemberAsync(UserId userId, CancellationToken cancellationToken = default) =>
        dbContext.TeamMemberships.AnyAsync(member => member.UserId == userId.Value, cancellationToken);
}
