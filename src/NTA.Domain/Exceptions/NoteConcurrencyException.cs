using NTA.Core.ValueObjects;

namespace NTA.Domain.Exceptions;

/// <summary>
/// Indicates that a note could not be changed because its revision no longer matched.
/// </summary>
public sealed class NoteConcurrencyException(NoteId noteId, long expectedRevision)
    : Exception($"Note '{noteId.Value}' does not have revision '{expectedRevision}'.")
{
    public NoteId NoteId { get; } = noteId;

    public long ExpectedRevision { get; } = expectedRevision;
}
