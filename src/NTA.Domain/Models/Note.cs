// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NTA.Core.ValueObjects;

namespace NTA.Domain.Models;

/// <summary>
/// Represents the note aggregate and enforces its content and lifecycle invariants.
/// </summary>
public sealed class Note
{
    public Note(
        NoteId id,
        UserId ownerId,
        string title,
        string content,
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
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public NoteId Id { get; }

    public UserId OwnerId { get; }

    public string Title { get; private set; }

    public string Content { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Note Create(
        UserId ownerId,
        string title,
        string content,
        DateTimeOffset now) =>
        new(NoteId.New(), ownerId, title, content, now, now);

    public void Update(
        string title,
        string content,
        DateTimeOffset updatedAt)
    {
        if (updatedAt < UpdatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(updatedAt), "Updated time cannot move backwards.");
        }

        Title = ValidateTitle(title);
        Content = content ?? throw new ArgumentNullException(nameof(content));
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
}
