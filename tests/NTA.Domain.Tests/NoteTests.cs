using NTA.Core.ValueObjects;
using NTA.Domain.Models;

namespace NTA.Domain.Tests;

/// <summary>
/// Verifies note aggregate invariants.
/// </summary>
public class NoteTests
{
    [Fact]
    public void Create_KeepsOwnerImmutableWhenContentChanges()
    {
        var now = DateTimeOffset.UtcNow;
        var note = Note.Create(
            new UserId(1),
            "Planning",
            "Draft the plan.",
            now);

        note.Update("Updated plan", "Updated draft.", now.AddMinutes(1));

        Assert.Equal(new UserId(1), note.OwnerId);
        Assert.Equal("Updated plan", note.Title);
    }
}
