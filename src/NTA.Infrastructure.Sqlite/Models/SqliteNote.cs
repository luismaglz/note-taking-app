namespace NTA.Infrastructure.Sqlite.Models;

/// <summary>
/// Represents the SQLite persistence record for a note.
/// </summary>
internal sealed class SqliteNote
{
    public Guid Id { get; set; }

    public int OwnerId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string Visibility { get; set; } = string.Empty;

    public int? TeamId { get; set; }

    public long CreatedAtUnixMilliseconds { get; set; }

    public long UpdatedAtUnixMilliseconds { get; set; }
}
