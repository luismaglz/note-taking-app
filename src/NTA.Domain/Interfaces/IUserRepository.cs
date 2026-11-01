// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;
using NTA.Domain.Models;

namespace NTA.Domain.Interfaces;

/// <summary>
/// Defines persistence operations for user aggregates.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by identifier.
/// </summary>
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all users.
    /// </summary>
    Task<IReadOnlyList<User>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user aggregate.
    /// </summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user aggregate.
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user aggregate.
    /// </summary>
    Task<bool> DeleteAsync(UserId id, CancellationToken cancellationToken = default);
}
