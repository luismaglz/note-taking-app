// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;
using NTA.Domain.Models;

namespace NTA.Domain.Interfaces;

/// <summary>
/// Defines persistence operations for team aggregates.
/// </summary>
public interface ITeamRepository
{
    /// <summary>
    /// Retrieves the teams of which a user is a member.
    /// </summary>
    Task<IReadOnlyList<Team>> ListByMemberAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a team by identifier.
    /// </summary>
    Task<Team?> GetByIdAsync(TeamId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all teams.
    /// </summary>
    Task<IReadOnlyList<Team>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a team aggregate.
    /// </summary>
    Task AddAsync(Team team, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a team aggregate.
    /// </summary>
    Task UpdateAsync(Team team, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a team aggregate.
    /// </summary>
    Task<bool> DeleteAsync(TeamId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the user belongs to at least one team.
    /// </summary>
    Task<bool> HasMemberAsync(UserId userId, CancellationToken cancellationToken = default);
}
