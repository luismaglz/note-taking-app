namespace NTA.Api.Contracts;

/// <summary>
/// Represents a note returned by the Users API.
/// </summary>
public sealed record NoteResponse(
    Guid Id,
    int OwnerId,
    string Title,
    string Content,
    NoteVisibilityContract Visibility,
    int? TeamId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
