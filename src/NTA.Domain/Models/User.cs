// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;

namespace NTA.Domain.Models;

/// <summary>
/// Represents a user aggregate that can own notes and authenticate with the sample APIs.
/// </summary>
public sealed class User(UserId id)
{
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public UserId Id { get; } = id;
}
