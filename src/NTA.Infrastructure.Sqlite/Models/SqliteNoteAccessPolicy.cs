namespace NTA.Infrastructure.Sqlite.Models;

/// <summary>
/// Represents the SQLite persistence record for a note access policy.
/// </summary>
internal sealed class SqliteNoteAccessPolicy
{
    public Guid NoteId { get; set; }
}
