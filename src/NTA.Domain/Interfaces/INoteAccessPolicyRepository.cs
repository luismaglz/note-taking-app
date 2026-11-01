using NTA.Core.ValueObjects;
using NTA.Domain.Models;

namespace NTA.Domain.Interfaces;

/// <summary>
/// Defines persistence operations for note access policy aggregates.
/// </summary>
public interface INoteAccessPolicyRepository
{
    /// <summary>
    /// Retrieves the policy for a note.
    /// </summary>
    Task<NoteAccessPolicy?> GetByNoteIdAsync(NoteId noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a policy for a note.
    /// </summary>
    Task AddAsync(NoteAccessPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the grants in an existing policy.
    /// </summary>
    Task UpdateAsync(NoteAccessPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the policy for a note.
    /// </summary>
    Task DeleteAsync(NoteId noteId, CancellationToken cancellationToken = default);
}
