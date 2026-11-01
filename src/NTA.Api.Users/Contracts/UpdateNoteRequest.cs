using NTA.Domain.Models;

namespace NTA.Api.Users.Contracts;

public sealed record UpdateNoteRequest(
    string Title,
    string Content,
    NoteVisibility Visibility,
    int? TeamId);
