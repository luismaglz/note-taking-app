namespace NTA.Infrastructure.Sqlite.Models;

/// <summary>
/// Represents the SQLite persistence record for a team member.
/// </summary>
internal sealed class SqliteTeamMembership
{
    public int TeamId { get; set; }

    public int UserId { get; set; }
}
