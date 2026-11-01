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

    Task AddAsync(Note note, CancellationToken cancellationToken = default);

    Task UpdateAsync(Note note, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(NoteId id, CancellationToken cancellationToken = default);
}
