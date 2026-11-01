using NTA.Api.Contracts;
using NTA.Domain.Models;

namespace NTA.Api.Users.Mappings;

/// <summary>
/// Translates between note access grants and Users API contracts.
/// </summary>
internal static class NoteAccessGrantContractMapper
{
    public static NoteAccessGrant ToDomain(CreateNoteAccessGrantRequest request) =>
        new(ToDomain(request.PrincipalKind), request.PrincipalId, ToDomain(request.Permission));

    public static NoteAccessGrantResponse ToContract(NoteAccessGrant grant) =>
        new(ToContract(grant.PrincipalKind), grant.PrincipalId, ToContract(grant.Permission));

    private static AccessPrincipalKind ToDomain(AccessPrincipalKindContract principalKind) => principalKind switch
    {
        AccessPrincipalKindContract.User => AccessPrincipalKind.User,
        AccessPrincipalKindContract.Team => AccessPrincipalKind.Team,
        _ => throw new ArgumentOutOfRangeException(nameof(principalKind), principalKind, "Unsupported principal kind.")
    };

    private static NotePermission ToDomain(NotePermissionContract permission) => permission switch
    {
        NotePermissionContract.Read => NotePermission.Read,
        NotePermissionContract.Write => NotePermission.Write,
        NotePermissionContract.Delete => NotePermission.Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(permission), permission, "Unsupported note permission.")
    };

    private static AccessPrincipalKindContract ToContract(AccessPrincipalKind principalKind) => principalKind switch
    {
        AccessPrincipalKind.User => AccessPrincipalKindContract.User,
        AccessPrincipalKind.Team => AccessPrincipalKindContract.Team,
        _ => throw new ArgumentOutOfRangeException(nameof(principalKind), principalKind, "Unsupported principal kind.")
    };

    private static NotePermissionContract ToContract(NotePermission permission) => permission switch
    {
        NotePermission.Read => NotePermissionContract.Read,
        NotePermission.Write => NotePermissionContract.Write,
        NotePermission.Delete => NotePermissionContract.Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(permission), permission, "Unsupported note permission.")
    };
}
