namespace NTA.Api.Contracts;

/// <summary>
/// Defines the editable payload for an existing note.
/// </summary>
public sealed record UpdateNoteRequest(
    string Title,
    string Content,
    NoteVisibilityContract Visibility,
    int? TeamId);
