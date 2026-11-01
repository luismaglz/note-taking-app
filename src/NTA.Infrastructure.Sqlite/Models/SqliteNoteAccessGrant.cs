namespace NTA.Infrastructure.Sqlite.Models;

/// <summary>
/// Represents the SQLite persistence record for one note access grant.
/// </summary>
internal sealed class SqliteNoteAccessGrant
{
    public Guid NoteId { get; set; }

    public string PrincipalKind { get; set; } = string.Empty;

    public int PrincipalId { get; set; }

    public string Permission { get; set; } = string.Empty;
}
