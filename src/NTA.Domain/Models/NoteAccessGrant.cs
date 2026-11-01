using NTA.Core.ValueObjects;

namespace NTA.Domain.Models;

/// <summary>
/// Represents one permission granted to a user or team principal.
/// </summary>
public sealed record NoteAccessGrant
{
    public NoteAccessGrant(AccessPrincipalKind principalKind, int principalId, NotePermission permission)
    {
        PrincipalKind = ValidatePrincipalKind(principalKind);
        PrincipalId = ValidatePrincipalId(principalKind, principalId);
        Permission = ValidatePermission(permission);
    }

    public AccessPrincipalKind PrincipalKind { get; }

    public int PrincipalId { get; }

    public NotePermission Permission { get; }

    private static AccessPrincipalKind ValidatePrincipalKind(AccessPrincipalKind principalKind)
    {
        if (!Enum.IsDefined(principalKind))
        {
            throw new ArgumentOutOfRangeException(nameof(principalKind), principalKind, "Unsupported principal kind.");
        }

        return principalKind;
    }

    private static int ValidatePrincipalId(AccessPrincipalKind principalKind, int principalId) =>
        principalKind switch
        {
            AccessPrincipalKind.User => new UserId(principalId).Value,
            AccessPrincipalKind.Team => new TeamId(principalId).Value,
            _ => throw new ArgumentOutOfRangeException(nameof(principalKind), principalKind, "Unsupported principal kind.")
        };

    private static NotePermission ValidatePermission(NotePermission permission)
    {
        if (!Enum.IsDefined(permission))
        {
            throw new ArgumentOutOfRangeException(nameof(permission), permission, "Unsupported note permission.");
        }

        return permission;
    }
}
