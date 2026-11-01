using NTA.Domain.Models;

namespace NTA.Api.Contracts;

public sealed record CreateNoteRequest(
    int OwnerId,
    string Title,
    string Content,
    NoteVisibility Visibility,
    int? TeamId);
