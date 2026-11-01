using Microsoft.EntityFrameworkCore;
using NTA.Core.ValueObjects;
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

    public async Task UpdateAsync(Note note, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);

        var entity = await dbContext.Notes
            .SingleOrDefaultAsync(existingNote => existingNote.Id == note.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Note '{note.Id.Value}' does not exist.");

        SqliteNoteMapper.Apply(note, entity);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(NoteId id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Notes
            .SingleOrDefaultAsync(note => note.Id == id.Value, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.Notes.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
