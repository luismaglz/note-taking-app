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
}
