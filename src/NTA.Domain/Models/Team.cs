// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;

namespace NTA.Domain.Models;

/// <summary>
/// Represents a team aggregate and its user membership.
/// </summary>
public sealed class Team
{
    private readonly HashSet<UserId> members;

    /// <summary>
    /// Initializes a team aggregate.
    /// </summary>
    public Team(TeamId id, string name, IEnumerable<UserId>? members = null)
    {
        Id = id;
        Name = ValidateName(name);
        this.members = members is null ? [] : new HashSet<UserId>(members);
    }

    /// <summary>
    /// Initializes a team aggregate with the legacy generated name convention.
    /// </summary>
    public Team(TeamId id, IEnumerable<UserId> members)
        : this(id, $"Team {id.Value}", members)
    {
    }

    /// <summary>
    /// Gets the team identifier.
    /// </summary>
    public TeamId Id { get; }

    /// <summary>
    /// Gets the team name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the users who belong to the team.
    /// </summary>
    public IReadOnlySet<UserId> Members => members;

    /// <summary>
    /// Gets the users who belong to the team.
    /// </summary>
    public IReadOnlySet<UserId> MemberIds => members;

    /// <summary>
    /// Determines whether a user belongs to this team.
    /// </summary>
    public bool HasMember(UserId userId) => members.Contains(userId);

    /// <summary>
    /// Changes the team name.
    /// </summary>
    public void Rename(string name) => Name = ValidateName(name);

    /// <summary>
    /// Adds a user to this team.
    /// </summary>
    public void AddMember(UserId userId)
    {
        if (!members.Add(userId))
        {
            throw new InvalidOperationException($"User '{userId.Value}' is already a member of this team.");
        }
    }

    /// <summary>
    /// Removes a user from this team.
    /// </summary>
    public void RemoveMember(UserId userId)
    {
        if (!members.Remove(userId))
        {
            throw new InvalidOperationException($"User '{userId.Value}' is not a member of this team.");
        }
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Team name is required.", nameof(name));
        }

        return name.Trim();
    }
}
