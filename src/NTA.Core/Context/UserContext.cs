// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;

namespace NTA.Core.Context;

/// <summary>
/// Represents the authenticated user and roles available for the current request.
/// </summary>
public sealed class UserContext
{
    private readonly HashSet<string> roles = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the current authenticated user identifier, when available.
    /// </summary>
    public UserId? UserId { get; private set; }

    /// <summary>
    /// Gets the roles granted to the current authenticated user.
    /// </summary>
    public IReadOnlySet<string> Roles => roles;

    /// <summary>
    /// Gets a value indicating whether the request has an authenticated user.
    /// </summary>
    public bool IsAuthenticated => UserId is not null;

    /// <summary>
    /// Gets the authenticated user identifier or throws when no user is available.
    /// </summary>
    public UserId GetRequiredUserId() =>
        UserId ?? throw new InvalidOperationException("The current request is not authenticated.");

    internal void Set(UserId userId, IEnumerable<string> userRoles)
    {
        ArgumentNullException.ThrowIfNull(userRoles);

        UserId = userId;
        roles.Clear();
        roles.UnionWith(userRoles);
    }
}
