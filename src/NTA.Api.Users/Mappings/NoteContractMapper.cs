using NTA.Api.Contracts;
using NTA.Domain.Models;

namespace NTA.Api.Users.Mappings;

/// <summary>
/// Translates between note aggregates and Users API contracts.
/// </summary>
internal static class NoteContractMapper
{
    public static NoteVisibility ToDomain(NoteVisibilityContract visibility) => visibility switch
    {
        NoteVisibilityContract.Private => NoteVisibility.Private,
        NoteVisibilityContract.Team => NoteVisibility.Team,
        NoteVisibilityContract.Public => NoteVisibility.Public,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, "Unsupported note visibility.")
    };

    public static NoteResponse ToContract(Note note) =>
        new(
            note.Id.Value,
            note.OwnerId.Value,
            note.Title,
            note.Content,
            ToContract(note.Visibility),
            note.TeamId?.Value,
            note.CreatedAt,
            note.UpdatedAt);

    private static NoteVisibilityContract ToContract(NoteVisibility visibility) => visibility switch
    {
        NoteVisibility.Private => NoteVisibilityContract.Private,
        NoteVisibility.Team => NoteVisibilityContract.Team,
        NoteVisibility.Public => NoteVisibilityContract.Public,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, "Unsupported note visibility.")
    };
}
