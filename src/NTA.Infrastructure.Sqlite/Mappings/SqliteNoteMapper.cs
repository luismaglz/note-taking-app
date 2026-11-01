using NTA.Core.ValueObjects;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Models;

namespace NTA.Infrastructure.Sqlite.Mappings;

/// <summary>
/// Translates between SQLite note records and note aggregates.
/// </summary>
internal static class SqliteNoteMapper
{
    public static SqliteNote ToPersistence(Note note) => new()
    {
        Id = note.Id.Value,
        OwnerId = note.OwnerId.Value,
        Title = note.Title,
        Content = note.Content,
        CreatedAtUnixMilliseconds = note.CreatedAt.ToUnixTimeMilliseconds(),
        UpdatedAtUnixMilliseconds = note.UpdatedAt.ToUnixTimeMilliseconds()
    };

    public static void Apply(Note note, SqliteNote persistence)
    {
        persistence.Title = note.Title;
        persistence.Content = note.Content;
        persistence.UpdatedAtUnixMilliseconds = note.UpdatedAt.ToUnixTimeMilliseconds();
    }

    public static Note ToDomain(SqliteNote persistence) =>
        new(
            new NoteId(persistence.Id),
            new UserId(persistence.OwnerId),
            persistence.Title,
            persistence.Content,
            DateTimeOffset.FromUnixTimeMilliseconds(persistence.CreatedAtUnixMilliseconds),
            DateTimeOffset.FromUnixTimeMilliseconds(persistence.UpdatedAtUnixMilliseconds));
}
