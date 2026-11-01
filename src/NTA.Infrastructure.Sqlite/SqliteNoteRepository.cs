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
            .ToListAsync(cancellationToken))
        .Select(SqliteNoteMapper.ToDomain)
        .ToArray();

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
