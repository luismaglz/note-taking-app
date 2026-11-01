using NTA.Domain.Models;

namespace NTA.Api.Users.Contracts;

public sealed record CreateNoteRequest(
    int OwnerId,
    string Title,
    string Content,
    NoteVisibility Visibility,
    int? TeamId);

public sealed record UpdateNoteRequest(
    string Title,
    string Content,
    NoteVisibility Visibility,
    int? TeamId);

public sealed record NoteResponse(
    Guid Id,
    int OwnerId,
    string Title,
    string Content,
    NoteVisibility Visibility,
    int? TeamId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
