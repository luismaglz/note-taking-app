// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;

namespace NTA.Domain.Models;

/// <summary>
/// Represents a user aggregate that can own notes and authenticate with the sample APIs.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Initializes a user aggregate.
    /// </summary>
    public User(UserId id, string name)
    {
        Id = id;
        Name = ValidateName(name);
    }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public UserId Id { get; }

    /// <summary>
    /// Gets the display name used to manage the user.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Changes the user's display name.
    /// </summary>
    public void Rename(string name) => Name = ValidateName(name);

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name is required.", nameof(name));
        }

        return name.Trim();
    }
}
