// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;

namespace NTA.Domain.Models;

public sealed class Note
{
    public Note(
        NoteId id,
        UserId ownerId,
        string title,
        string content,
        NoteVisibility visibility,
        TeamId? teamId,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (createdAt > updatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(createdAt), "Created time cannot be after updated time.");
        }

        Id = id;
        OwnerId = ownerId;
        Title = ValidateTitle(title);
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Visibility = visibility;
        TeamId = ValidateTeamScope(visibility, teamId);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public NoteId Id { get; }

    public UserId OwnerId { get; }

    public string Title { get; private set; }

    public string Content { get; private set; }

    public NoteVisibility Visibility { get; private set; }

    public TeamId? TeamId { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Note Create(
        UserId ownerId,
        string title,
        string content,
        NoteVisibility visibility,
        TeamId? teamId,
        DateTimeOffset now) =>
        new(NoteId.New(), ownerId, title, content, visibility, teamId, now, now);

    public bool CanBeViewedBy(UserId userId, IReadOnlySet<TeamId> teamIds) =>
        OwnerId == userId ||
        Visibility == NoteVisibility.Public ||
        Visibility == NoteVisibility.Team && TeamId is not null && teamIds.Contains(TeamId.Value);

    public void Update(
        string title,
        string content,
        NoteVisibility visibility,
        TeamId? teamId,
        DateTimeOffset updatedAt)
    {
        if (updatedAt < UpdatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(updatedAt), "Updated time cannot move backwards.");
        }

        Title = ValidateTitle(title);
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Visibility = visibility;
        TeamId = ValidateTeamScope(visibility, teamId);
        UpdatedAt = updatedAt;
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        return title.Trim();
    }

    private static TeamId? ValidateTeamScope(NoteVisibility visibility, TeamId? teamId) => visibility switch
    {
        NoteVisibility.Team when teamId is null => throw new ArgumentException("Team notes require a team id.", nameof(teamId)),
        NoteVisibility.Team => teamId,
        NoteVisibility.Private or NoteVisibility.Public when teamId is not null =>
            throw new ArgumentException("Only team notes can have a team id.", nameof(teamId)),
        NoteVisibility.Private or NoteVisibility.Public => null,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, "Unsupported note visibility.")
    };
}
