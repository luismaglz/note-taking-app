using Microsoft.EntityFrameworkCore;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;

namespace NTA.Infrastructure.Sqlite;

public sealed class SqliteNoteRepository(NoteDbContext dbContext) : INoteRepository
{
    public async Task<Note?> GetByIdAsync(NoteId id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Notes
            .AsNoTracking()
            .SingleOrDefaultAsync(note => note.Id == id.Value, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Note>> ListAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.Notes
            .AsNoTracking()
            .OrderByDescending(note => note.UpdatedAtUnixMilliseconds)
            .ToListAsync(cancellationToken))
        .Select(ToDomain)
        .ToArray();

    public async Task AddAsync(Note note, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);

        await dbContext.Notes.AddAsync(ToEntity(note), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Note note, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(note);

        var entity = await dbContext.Notes
            .SingleOrDefaultAsync(existingNote => existingNote.Id == note.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Note '{note.Id.Value}' does not exist.");

        entity.Title = note.Title;
        entity.Content = note.Content;
        entity.Visibility = note.Visibility.ToString();
        entity.TeamId = note.TeamId?.Value;
        entity.UpdatedAtUnixMilliseconds = note.UpdatedAt.ToUnixTimeMilliseconds();

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

    private static NoteEntity ToEntity(Note note) => new()
    {
        Id = note.Id.Value,
        OwnerId = note.OwnerId.Value,
        Title = note.Title,
        Content = note.Content,
        Visibility = note.Visibility.ToString(),
        TeamId = note.TeamId?.Value,
        CreatedAtUnixMilliseconds = note.CreatedAt.ToUnixTimeMilliseconds(),
        UpdatedAtUnixMilliseconds = note.UpdatedAt.ToUnixTimeMilliseconds()
    };

    private static Note ToDomain(NoteEntity entity) =>
        new(
            new NoteId(entity.Id),
            new UserId(entity.OwnerId),
            entity.Title,
            entity.Content,
            Enum.Parse<NoteVisibility>(entity.Visibility),
            entity.TeamId is null ? null : new TeamId(entity.TeamId.Value),
            DateTimeOffset.FromUnixTimeMilliseconds(entity.CreatedAtUnixMilliseconds),
            DateTimeOffset.FromUnixTimeMilliseconds(entity.UpdatedAtUnixMilliseconds));
}
