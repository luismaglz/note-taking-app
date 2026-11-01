namespace NTA.Core.ValueObjects;

public readonly record struct UserId
{
    public int Value { get; }

    public UserId(int value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "User id must be greater than zero.");
        }

        Value = value;
    }
}
