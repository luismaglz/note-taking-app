using NTA.Core.ValueObjects;
using NTA.Domain.Models;

namespace NTA.Domain.Tests;

/// <summary>
/// Verifies note access policy grant invariants and permission checks.
/// </summary>
public sealed class NoteAccessPolicyTests
{
    [Fact]
    public void GrantAccess_RejectsDuplicateGrant()
    {
        var policy = NoteAccessPolicy.Create(new NoteId(Guid.NewGuid()));
        var grant = new NoteAccessGrant(AccessPrincipalKind.User, 2, NotePermission.Read);

        policy.GrantAccess(grant);

        Assert.Throws<InvalidOperationException>(() => policy.GrantAccess(grant));
    }

    [Fact]
    public void Create_RejectsAnEmptyPolicyIdentity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NoteAccessPolicy.Create(default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Grant_RejectsInvalidPrincipalId(int principalId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new NoteAccessGrant(AccessPrincipalKind.User, principalId, NotePermission.Read));
    }

    [Fact]
    public void HasPermission_MatchesDirectUsersAndMemberTeamsForExactPermission()
    {
        var policy = NoteAccessPolicy.Create(new NoteId(Guid.NewGuid()));
        policy.GrantAccess(new NoteAccessGrant(AccessPrincipalKind.User, 2, NotePermission.Read));
        policy.GrantAccess(new NoteAccessGrant(AccessPrincipalKind.Team, 10, NotePermission.Write));

        Assert.True(policy.HasPermission(new UserId(2), new HashSet<TeamId>(), NotePermission.Read));
        Assert.False(policy.HasPermission(new UserId(2), new HashSet<TeamId>(), NotePermission.Write));
        Assert.True(policy.HasPermission(new UserId(3), new HashSet<TeamId> { new(10) }, NotePermission.Write));
        Assert.False(policy.HasPermission(new UserId(4), new HashSet<TeamId>(), NotePermission.Write));
    }
}
