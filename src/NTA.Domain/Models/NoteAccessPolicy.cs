using NTA.Core.ValueObjects;

namespace NTA.Domain.Models;

/// <summary>
/// Represents the access grants for one note.
/// </summary>
public sealed class NoteAccessPolicy
{
    private readonly HashSet<NoteAccessGrant> grants;

    public NoteAccessPolicy(NoteId noteId, IEnumerable<NoteAccessGrant> grants)
    {
        ArgumentNullException.ThrowIfNull(grants);
        if (noteId.Value == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(noteId), "Policy note id must not be empty.");
        }

        var grantList = grants.ToArray();

        Id = noteId;
        this.grants = grantList.ToHashSet();
        if (this.grants.Count != grantList.Length)
        {
            throw new ArgumentException("Duplicate access grants are not allowed.", nameof(grants));
        }
    }

    public NoteId Id { get; }

    public IReadOnlySet<NoteAccessGrant> Grants => grants;

    public static NoteAccessPolicy Create(NoteId noteId) => new(noteId, []);

    public void GrantAccess(NoteAccessGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);

        if (!grants.Add(grant))
        {
            throw new InvalidOperationException("The access grant already exists.");
        }
    }

    public void RevokeAccess(NoteAccessGrant grant)
    {
        ArgumentNullException.ThrowIfNull(grant);
        grants.Remove(grant);
    }

    public bool HasPermission(
        UserId userId,
        IReadOnlySet<TeamId> teamIds,
        NotePermission permission)
    {
        ArgumentNullException.ThrowIfNull(teamIds);

        return grants.Any(grant =>
            grant.Permission == permission &&
            (grant.PrincipalKind == AccessPrincipalKind.User && grant.PrincipalId == userId.Value ||
             grant.PrincipalKind == AccessPrincipalKind.Team && teamIds.Any(teamId => teamId.Value == grant.PrincipalId)));
    }
}
