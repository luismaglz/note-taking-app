// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;
using NTA.Domain.Models;

namespace NTA.Domain.Interfaces;

/// <summary>
/// Defines persistence operations for note aggregates.
/// </summary>
public interface INoteRepository
{
    Task<Note?> GetByIdAsync(NoteId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Note>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves notes visible to a user through ownership or read access grants.
    /// </summary>
    Task<IReadOnlyList<Note>> ListVisibleToAsync(UserId userId, CancellationToken cancellationToken = default);

    Task AddAsync(Note note, CancellationToken cancellationToken = default);

    Task<long> UpdateAsync(
        Note note,
        long expectedRevision,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        NoteId id,
        long expectedRevision,
        CancellationToken cancellationToken = default);
}
