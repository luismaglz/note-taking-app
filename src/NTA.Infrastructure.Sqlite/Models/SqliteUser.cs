namespace NTA.Infrastructure.Sqlite.Models;

/// <summary>
/// Represents the SQLite persistence record for a user.
/// </summary>
internal sealed class SqliteUser
{
    /// <summary>
    /// Gets or sets the integer user identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string Name { get; set; } = null!;
}
