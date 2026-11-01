// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;

namespace NTA.Domain.Models;

/// <summary>
/// Represents a team and the users that are its members.
/// </summary>
public sealed class Team
{
    private readonly HashSet<UserId> memberIds;

    public Team(TeamId id, IEnumerable<UserId> memberIds)
    {
        ArgumentNullException.ThrowIfNull(memberIds);

        Id = id;
        this.memberIds = memberIds.ToHashSet();
    }

    public TeamId Id { get; }

    public IReadOnlySet<UserId> MemberIds => memberIds;

    public bool HasMember(UserId userId) => memberIds.Contains(userId);
}
