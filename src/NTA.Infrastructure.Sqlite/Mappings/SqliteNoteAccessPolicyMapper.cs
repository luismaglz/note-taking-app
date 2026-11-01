using NTA.Core.ValueObjects;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite.Models;

namespace NTA.Infrastructure.Sqlite.Mappings;

/// <summary>
/// Translates between SQLite policy records and note access policy aggregates.
/// </summary>
internal static class SqliteNoteAccessPolicyMapper
{
    public static SqliteNoteAccessPolicy ToPersistence(NoteAccessPolicy policy) => new()
    {
        NoteId = policy.Id.Value
    };

    public static SqliteNoteAccessGrant ToPersistence(NoteId noteId, NoteAccessGrant grant) => new()
    {
        NoteId = noteId.Value,
        PrincipalKind = grant.PrincipalKind.ToString(),
        PrincipalId = grant.PrincipalId,
        Permission = grant.Permission.ToString()
    };

    public static NoteAccessPolicy ToDomain(
        SqliteNoteAccessPolicy policy,
        IEnumerable<SqliteNoteAccessGrant> grants) =>
        new(
            new NoteId(policy.NoteId),
            grants.Select(grant => new NoteAccessGrant(
                Enum.Parse<AccessPrincipalKind>(grant.PrincipalKind),
                grant.PrincipalId,
                Enum.Parse<NotePermission>(grant.Permission))));
}
