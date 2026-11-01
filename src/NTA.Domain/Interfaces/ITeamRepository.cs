// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;
using NTA.Domain.Models;

namespace NTA.Domain.Interfaces;

/// <summary>
/// Defines membership lookups for team aggregates.
/// </summary>
public interface ITeamRepository
{
    /// <summary>
    /// Retrieves the teams of which a user is a member.
    /// </summary>
    Task<IReadOnlyList<Team>> ListByMemberAsync(UserId userId, CancellationToken cancellationToken = default);
}
