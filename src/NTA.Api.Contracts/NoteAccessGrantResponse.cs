namespace NTA.Api.Contracts;

/// <summary>
/// Represents a note access grant returned by the Users API.
/// </summary>
public sealed record NoteAccessGrantResponse(
    AccessPrincipalKindContract PrincipalKind,
    int PrincipalId,
    NotePermissionContract Permission);
