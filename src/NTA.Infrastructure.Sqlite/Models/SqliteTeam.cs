namespace NTA.Infrastructure.Sqlite.Models;

/// <summary>
/// Represents the SQLite persistence record for a team.
/// </summary>
internal sealed class SqliteTeam
{
    /// <summary>
    /// Gets or sets the team identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the team name.
    /// </summary>
    public string Name { get; set; } = null!;
}
