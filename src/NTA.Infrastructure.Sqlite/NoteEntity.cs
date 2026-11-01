namespace NTA.Infrastructure.Sqlite;

internal sealed class NoteEntity
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
