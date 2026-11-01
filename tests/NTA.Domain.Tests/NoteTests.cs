using NTA.Core.ValueObjects;
using NTA.Domain.Models;

namespace NTA.Domain.Tests;

public class NoteTests
{
    [Fact]
    public void Create_ThrowsWhenTeamNoteHasNoTeam()
    {
        var action = () => Note.Create(
            new UserId(1),
            "Planning",
            "Draft the plan.",
            NoteVisibility.Team,
            null,
            DateTimeOffset.UtcNow);

        var exception = Assert.Throws<ArgumentException>(action);

        Assert.Equal("teamId", exception.ParamName);
    }

    [Fact]
    public void CanBeViewedBy_AllowsOwnerPublicAndMatchingTeamMembers()
    {
        var now = DateTimeOffset.UtcNow;
        var teamNote = Note.Create(
            new UserId(1),
            "Team plan",
            "Draft the plan.",
            NoteVisibility.Team,
            new TeamId(20),
            now);

        Assert.True(teamNote.CanBeViewedBy(new UserId(1), new HashSet<TeamId>()));
        Assert.True(teamNote.CanBeViewedBy(new UserId(2), new HashSet<TeamId> { new(20) }));
        Assert.False(teamNote.CanBeViewedBy(new UserId(2), new HashSet<TeamId>()));
    }
}
