// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace NTA.Core.ValueObjects;

public struct TeamId
{
    public int Value { get; private set; }

    public TeamId(int teamId)
    {
        Value = teamId;
    }
}
