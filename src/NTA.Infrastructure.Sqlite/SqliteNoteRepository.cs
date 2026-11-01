using Microsoft.EntityFrameworkCore;
using NTA.Core.ValueObjects;
using NTA.Domain.Exceptions;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Mappings;

namespace NTA.Infrastructure.Sqlite;

/// <summary>
/// Persists note aggregates in SQLite while translating them at the infrastructure boundary.
/// </summary>
public sealed class SqliteNoteRepository(NoteDbContext dbContext) : INoteRepository
{
    public async Task<Note?> GetByIdAsync(NoteId id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Notes
            .AsNoTracking()
            .SingleOrDefaultAsync(note => note.Id == id.Value, cancellationToken);

        return entity is null ? null : SqliteNoteMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Note>> ListAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.Notes
            .AsNoTracking()
            .OrderByDescending(note => note.UpdatedAtUnixMilliseconds)
            .ThenBy(note => note.Id)
            .ToListAsync(cancellationToken))
        .Select(SqliteNoteMapper.ToDomain)
        .ToArray();

    public async Task<IReadOnlyList<Note>> ListVisibleToAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var directlyReadableNoteIds = dbContext.NoteAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.PrincipalKind == nameof(AccessPrincipalKind.User) &&
                grant.PrincipalId == userId.Value &&
                grant.Permission == nameof(NotePermission.Read))
            .Select(grant => grant.NoteId);
        var teamReadableNoteIds =
            from grant in dbContext.NoteAccessGrants.AsNoTracking()
            join membership in dbContext.TeamMemberships.AsNoTracking()
                on grant.PrincipalId equals membership.TeamId
            where grant.PrincipalKind == nameof(AccessPrincipalKind.Team) &&
                  membership.UserId == userId.Value &&
                  grant.Permission == nameof(NotePermission.Read)
            select grant.NoteId;
        var visibleNoteIds = dbContext.Notes
            .AsNoTracking()
            .Where(note => note.OwnerId == userId.Value)
            .Select(note => note.Id)
            .Union(directlyReadableNoteIds)
            .Union(teamReadableNoteIds);

        return (await dbContext.Notes
            .AsNoTracking()
            .Where(note => visibleNoteIds.Contains(note.Id))
            .OrderByDescending(note => note.UpdatedAtUnixMilliseconds)
            .ThenBy(note => note.Id)
            .ToListAsync(cancellationToken))
            .Select(SqliteNoteMapper.ToDomain)
            .ToArray();
    }

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);

        await dbContext.Notes.AddAsync(SqliteNoteMapper.ToPersistence(note), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> UpdateAsync(
        Note note,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);

        var nextRevision = checked(expectedRevision + 1);
        var updatedCount = await dbContext.Notes
            .Where(existingNote =>
                existingNote.Id == note.Id.Value &&
                existingNote.Revision == expectedRevision)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(entity => entity.Title, note.Title)
                    .SetProperty(entity => entity.Content, note.Content)
                    .SetProperty(entity => entity.UpdatedAtUnixMilliseconds, note.UpdatedAt.ToUnixTimeMilliseconds())
                    .SetProperty(entity => entity.Revision, nextRevision),
                cancellationToken);

        if (updatedCount != 1)
        {
            throw new NoteConcurrencyException(note.Id, expectedRevision);
        }

        return nextRevision;
    }

    public async Task DeleteAsync(
        NoteId id,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        var deletedCount = await dbContext.Notes
            .Where(note => note.Id == id.Value && note.Revision == expectedRevision)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount != 1)
        {
            throw new NoteConcurrencyException(id, expectedRevision);
        }
    }
}
