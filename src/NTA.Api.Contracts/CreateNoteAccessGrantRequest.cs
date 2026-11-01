namespace NTA.Api.Contracts;

/// <summary>
/// Defines the payload used to grant a principal access to a note.
/// </summary>
public sealed record CreateNoteAccessGrantRequest(
    AccessPrincipalKindContract PrincipalKind,
    int PrincipalId,
    NotePermissionContract Permission);
