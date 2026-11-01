using Microsoft.EntityFrameworkCore;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Mappings;

namespace NTA.Infrastructure.Sqlite;

/// <summary>
/// Persists note access policy aggregates in SQLite.
/// </summary>
public sealed class SqliteNoteAccessPolicyRepository(NoteDbContext dbContext) : INoteAccessPolicyRepository
{
    public async Task<NoteAccessPolicy?> GetByNoteIdAsync(
        NoteId noteId,
        CancellationToken cancellationToken = default)
    {
        var persistence = await dbContext.NoteAccessPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(policy => policy.NoteId == noteId.Value, cancellationToken);

        if (persistence is null)
        {
            return null;
        }

        var grants = await dbContext.NoteAccessGrants
            .AsNoTracking()
            .Where(grant => grant.NoteId == noteId.Value)
            .ToListAsync(cancellationToken);

        return SqliteNoteAccessPolicyMapper.ToDomain(persistence, grants);
    }

    public async Task AddAsync(NoteAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        await dbContext.NoteAccessPolicies.AddAsync(
            SqliteNoteAccessPolicyMapper.ToPersistence(policy),
            cancellationToken);
        await dbContext.NoteAccessGrants.AddRangeAsync(
            policy.Grants.Select(grant => SqliteNoteAccessPolicyMapper.ToPersistence(policy.Id, grant)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(NoteAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var persistence = await dbContext.NoteAccessPolicies
            .SingleOrDefaultAsync(existingPolicy => existingPolicy.NoteId == policy.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Policy for note '{policy.Id.Value}' does not exist.");
        var grants = await dbContext.NoteAccessGrants
            .Where(grant => grant.NoteId == persistence.NoteId)
            .ToListAsync(cancellationToken);

        dbContext.NoteAccessGrants.RemoveRange(grants);
        await dbContext.NoteAccessGrants.AddRangeAsync(
            policy.Grants.Select(grant => SqliteNoteAccessPolicyMapper.ToPersistence(policy.Id, grant)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(NoteId noteId, CancellationToken cancellationToken = default)
    {
        var policy = await dbContext.NoteAccessPolicies
            .SingleOrDefaultAsync(existingPolicy => existingPolicy.NoteId == noteId.Value, cancellationToken);

        if (policy is null)
        {
            return;
        }

        var grants = await dbContext.NoteAccessGrants
            .Where(grant => grant.NoteId == noteId.Value)
            .ToListAsync(cancellationToken);
        dbContext.NoteAccessGrants.RemoveRange(grants);
        dbContext.NoteAccessPolicies.Remove(policy);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
