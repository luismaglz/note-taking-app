using NTA.Domain.Models;

namespace NTA.Api.Contracts;

public sealed record NoteResponse(
    Guid Id,
    int OwnerId,
    string Title,
    string Content,
    NoteVisibility Visibility,
    int? TeamId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
