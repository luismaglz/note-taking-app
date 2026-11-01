using NTA.Domain.Models;

namespace NTA.Api.Contracts;

public sealed record UpdateNoteRequest(
    string Title,
    string Content,
    NoteVisibility Visibility,
    int? TeamId);
