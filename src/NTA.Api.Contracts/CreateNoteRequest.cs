namespace NTA.Api.Contracts;

/// <summary>
/// Defines the payload used to create a note.
/// </summary>
public sealed record CreateNoteRequest(
    string Title,
    string Content);
