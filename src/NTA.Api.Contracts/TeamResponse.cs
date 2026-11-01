namespace NTA.Api.Contracts;

/// <summary>
/// Represents a team returned by the management API.
/// </summary>
public sealed record TeamResponse(int Id, string Name, IReadOnlyList<int> MemberIds);
