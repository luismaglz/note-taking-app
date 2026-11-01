namespace NTA.Domain.Models;

/// <summary>
/// Identifies an operation that can be granted for a note.
/// </summary>
public enum NotePermission
{
    Read,
    Write,
    Delete
}
