using NTA.Api.Contracts;
using NTA.Domain.Models;

namespace NTA.Api.Users.Mappings;

/// <summary>
/// Translates between note aggregates and Users API contracts.
/// </summary>
internal static class NoteContractMapper
{
    public static NoteResponse ToContract(Note note) =>
        new(
            note.Id.Value,
            note.OwnerId.Value,
            note.Title,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt);
}
