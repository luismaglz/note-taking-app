// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NTA.Core.ValueObjects;

/// <summary>
/// Represents a validated identifier for a team aggregate.
/// </summary>
public readonly record struct TeamId
{
    public int Value { get; }

    public TeamId(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Team id must be greater than zero.");
        }

        Value = value;
    }
}
