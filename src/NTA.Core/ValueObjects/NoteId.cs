namespace NTA.Core.ValueObjects;

/// <summary>
/// Represents a validated identifier for a note aggregate.
/// </summary>
public readonly record struct NoteId
{
    public Guid Value { get; }

    public NoteId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Note id must not be empty.");
        }

        Value = value;
    }

    public static NoteId New() => new(Guid.NewGuid());
}
