using System.Globalization;
using Microsoft.Extensions.Primitives;

namespace NTA.Api.Users;

/// <summary>
/// Formats and parses strong entity tags used for note revisions.
/// </summary>
internal static class NoteEtag
{
    public static string Format(long revision)
    {
        if (revision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        return $"\"{revision.ToString(CultureInfo.InvariantCulture)}\"";
    }

    public static bool TryParseExpectedRevision(StringValues values, out long revision)
    {
        revision = default;

        if (values.Count != 1)
        {
            return false;
        }

        var value = values[0]?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length < 3 || value[0] != '"' || value[^1] != '"')
        {
            return false;
        }

        return long.TryParse(
            value.AsSpan(1, value.Length - 2),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out revision) &&
            revision > 0 &&
            string.Equals(value, Format(revision), StringComparison.Ordinal);
    }
}
